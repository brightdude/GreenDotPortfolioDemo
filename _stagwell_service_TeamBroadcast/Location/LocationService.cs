using Ardalis.GuardClauses;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    //TODO: Need refactoring
    public class LocationService
    {
        private readonly IBreezyCosmosService _breezyCosmosService;
        private readonly IAuthorisationService _authService;

        public LocationService(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        #region Helpers

        /// <summary>
        /// Returns a list of all location items of the specified type with status of Active.
        /// </summary>
        public async Task<HttpResponseData> LocationRetrieveAll<T>(HttpRequestData req, FunctionContext executionContext, string functionName, string parentId = null) where T : LocationItem
        {
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);

            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var locationType = LocationType.unknown;
                string parentField = null;
                switch (typeof(T).Name)
                {
                    case nameof(CountryLocation):
                        locationType = LocationType.country;
                        break;
                    case nameof(StateLocation):
                        locationType = LocationType.state;
                        parentField = "countryId";
                        break;
                    case nameof(RegionLocation):
                        locationType = LocationType.region;
                        parentField = "stateId";
                        break;
                    case nameof(SubRegionLocation):
                        locationType = LocationType.subregion;
                        parentField = "regionId";
                        break;
                    case nameof(BuildingLocation):
                        locationType = LocationType.building;
                        parentField = "subRegionId";
                        break;
                }

                var sql = $"SELECT * FROM c WHERE c.status = 'Active' AND c.type = '{locationType}'";
                if (parentId != null && parentField != null)
                {
                    sql += $" AND c.{parentField} = '{parentId}'";
                }
                var locations = await _breezyCosmosService.GetList<T>(breezyContainers.Locations, new QueryDefinition(sql));

                logger.LogInformation("Function {functionName} succeeded!", functionName);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(LookupBase.ToJsonArrayString(locations));
                return response;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Returns a location item with the provided id, or 404 if not found.
        /// </summary>
        public async Task<HttpResponseData> LocationRetrieve<T>(HttpRequestData req, FunctionContext executionContext, string functionName, string id, IDictionary<string, string> pathParams) where T : LocationItem
        {
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var locationType = GetLocationType<T>();
                var sql = $"SELECT * FROM c WHERE c.type = '{locationType}'";
                foreach (var kvp in pathParams)
                {
                    sql += $" AND c.{kvp.Key} = '{kvp.Value}'";
                }
                var queryDef = new QueryDefinition(sql);
                var locationItem = await _breezyCosmosService.GetItem<T>(breezyContainers.Locations, queryDef);

                if (locationItem == null) return await req.NotFoundResponse(logger, $"A {locationType} item with id '{id}' could not be found");
                locationItem.ActiveRelations = (await GetActiveRelations(locationType, id)).ToArray();
                logger.LogInformation("Function {functionName} succeeded!", functionName);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(locationItem.ToJsonString());
                return response;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Gets a summary of documents that are related to a location item.
        /// </summary>
        private async Task<IEnumerable<ActiveRelationItem>> GetActiveRelations(LocationType locationType, string id)
        {
            var relationItems = new List<ActiveRelationItem>();
            QueryDefinition queryDef;

            switch (locationType)
            {
                case LocationType.building:
                    queryDef = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.buildingId = @id").WithParameter("@id", id);
                    relationItems.Add(new ActiveRelationItem() { Name = "Facility", ActiveCount = await _breezyCosmosService.GetItem<int>("Facilities", queryDef) });
                    break;
                case LocationType.subregion:
                    queryDef = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.status = 'Active' AND c.type = 'building' AND c.subRegionId = @id").WithParameter("@id", id);
                    relationItems.Add(new ActiveRelationItem() { Name = "Building", ActiveCount = await _breezyCosmosService.GetItem<int>("Locations", queryDef) });
                    break;
                case LocationType.region:
                    queryDef = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.status = 'Active' AND c.type = 'subregion' AND c.regionId = @id").WithParameter("@id", id);
                    relationItems.Add(new ActiveRelationItem() { Name = "SubRegion", ActiveCount = await _breezyCosmosService.GetItem<int>("Locations", queryDef) });
                    break;
                case LocationType.state:
                    queryDef = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.status = 'Active' AND c.type = 'region' AND c.stateId = @id").WithParameter("@id", id);
                    relationItems.Add(new ActiveRelationItem() { Name = "Region", ActiveCount = await _breezyCosmosService.GetItem<int>("Locations", queryDef) });
                    break;
                case LocationType.country:
                    queryDef = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.status = 'Active' AND c.type = 'state' AND c.countryId = @id").WithParameter("@id", id);
                    relationItems.Add(new ActiveRelationItem() { Name = "State", ActiveCount = await _breezyCosmosService.GetItem<int>("Locations", queryDef) });
                    break;
            }

            return relationItems;
        }

        private static LocationType GetLocationType<T>() where T : LocationItem
        {
            var locationType = LocationType.unknown;
            switch (typeof(T).Name)
            {
                case nameof(CountryLocation):
                    locationType = LocationType.country;
                    break;
                case nameof(StateLocation):
                    locationType = LocationType.state;
                    break;
                case nameof(RegionLocation):
                    locationType = LocationType.region;
                    break;
                case nameof(SubRegionLocation):
                    locationType = LocationType.subregion;
                    break;
                case nameof(BuildingLocation):
                    locationType = LocationType.building;
                    break;
            }
            return locationType;
        }

        /// <summary>
        /// Inserts a new location item. Returns a 201 response or 409 if the id already exists.
        /// </summary>
        public async Task<HttpResponseData> LocationInsert<T, TParams>(HttpRequestData req, FunctionContext executionContext, string functionName, IDictionary<string, string> pathParams) where T : LocationItem where TParams : LocationItemParams
        {
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            LocationItem locationItem;
            try
            {
                locationItem = typeof(T).Name switch
                {
                    nameof(CountryLocation) => new CountryLocation { Type = LocationType.country },
                    nameof(StateLocation) => new StateLocation { Type = LocationType.state },
                    nameof(RegionLocation) => new RegionLocation { Type = LocationType.region },
                    nameof(SubRegionLocation) => new SubRegionLocation { Type = LocationType.subregion },
                    nameof(BuildingLocation) => new BuildingLocation { Type = LocationType.building },
                    _ => throw new ArgumentException("Could not recognise location type"),
                };
                locationItem.Status = "Active";

                var locationItemParams = await req.ReadFromJsonAsync<TParams>();
                foreach (var propInfo in locationItem.GetType().GetProperties())
                {
                    if (propInfo.Name == "type" || propInfo.Name == "status") continue;
                    var matchingProp = locationItemParams.GetType().GetProperty(propInfo.Name);
                    if (matchingProp != null) propInfo.SetValue(locationItem, matchingProp.GetValue(locationItemParams, null));
                }

                var validationError = locationItem.Validate();
                if (!string.IsNullOrWhiteSpace(validationError)) return await req.BadRequestResponse(logger, validationError);
                validationError = await ValidateRequest(executionContext, locationItem, pathParams, true);
                if (!string.IsNullOrWhiteSpace(validationError)) return await req.BadRequestResponse(logger, validationError);
            }
            catch (Exception ex)
            {
                return await req.BadRequestResponse(logger, ex);
            }

            try
            {
                var createResponse = await _breezyCosmosService.CreateItem(breezyContainers.Locations, locationItem as T);
                logger.LogInformation("Function {functionName} succeeded!", functionName);

                var retrievedResponse = req.CreateResponse(HttpStatusCode.Created);
                await retrievedResponse.WriteStringAsync(createResponse.Resource.ToJsonString());
                return retrievedResponse;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Updates an existing location item with the provided id. Returns a 200 response, or 404 if not found.
        /// </summary>
        public async Task<HttpResponseData> LocationUpdate<T, TParams>(HttpRequestData req, FunctionContext executionContext, string functionName, string id, IDictionary<string, string> pathParams) where T : LocationItem where TParams : LocationItemParams
        {
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            LocationItem locationItem;
            try
            {
                switch (typeof(T).Name)
                {
                    case nameof(CountryLocation):
                        locationItem = new CountryLocation();
                        locationItem.Type = LocationType.country;
                        break;
                    case nameof(StateLocation):
                        locationItem = new StateLocation();
                        locationItem.Type = LocationType.state;
                        break;
                    case nameof(RegionLocation):
                        locationItem = new RegionLocation();
                        locationItem.Type = LocationType.region;
                        break;
                    case nameof(SubRegionLocation):
                        locationItem = new SubRegionLocation();
                        locationItem.Type = LocationType.subregion;
                        break;
                    case nameof(BuildingLocation):
                        locationItem = new BuildingLocation();
                        locationItem.Type = LocationType.building;
                        break;
                    default:
                        throw new ArgumentException("Could not recognise location type");
                }

                locationItem.Status = "Active";

                var locationItemParams = await req.ReadFromJsonAsync<TParams>();
                foreach (var propInfo in locationItem.GetType().GetProperties())
                {
                    if (propInfo.Name == "type" || propInfo.Name == "status") continue;
                    var matchingProp = locationItemParams.GetType().GetProperty(propInfo.Name);
                    if (matchingProp != null) propInfo.SetValue(locationItem, matchingProp.GetValue(locationItemParams, null));
                }

                var validationError = locationItem.Validate();
                if (!string.IsNullOrWhiteSpace(validationError)) return await req.BadRequestResponse(logger, validationError);
                validationError = await ValidateRequest(executionContext, locationItem, pathParams, false);
                if (!string.IsNullOrWhiteSpace(validationError)) return await req.BadRequestResponse(logger, validationError);
            }
            catch (Exception ex)
            {
                return await req.BadRequestResponse(logger, ex);
            }

            try
            {
                // Fetch the current item
                var existingLocation = await _breezyCosmosService.GetItem<T>(breezyContainers.Locations, id, id);
                if (existingLocation == null)
                {
                    return await req.NotFoundResponse(logger, "Locations", id);
                }

                // Update the item
                var upsertResponse = await _breezyCosmosService.UpsertItem(breezyContainers.Locations, locationItem as T, id);

                // Update any parent items, if required
                await CascadeUpdate(existingLocation, upsertResponse.Resource, logger);

                logger.LogInformation("Function {functionName} succeeded!", functionName);

                var response = req.CreateResponse(HttpStatusCode.OK);
                var obj = Newtonsoft.Json.Linq.JObject.FromObject(upsertResponse.Resource);
                await response.WriteStringAsync(upsertResponse.Resource.ToJsonString());
                return response;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }


        private static bool HasPropertyChanged<T>(T original, T updated, String propertyName)
        {
            var originalValue = original.GetType().GetProperty(propertyName).GetValue(original).ToString();
            var updatedValue = updated.GetType().GetProperty(propertyName).GetValue(updated).ToString();
            if (originalValue != updatedValue)
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Checks if the name of a location item has changed, and if so updates any other location item referencing the same name.
        /// </summary>
        private async Task CascadeUpdate<T>(T original, T updated, ILogger logger) where T : LocationItem
        {
            switch (typeof(T).Name)
            {
                case nameof(CountryLocation):
                    if (HasPropertyChanged(original, updated, "Name"))
                    {
                        logger.LogInformation($"{updated.Type} cascade change for {original.Name}");
                        await CascadeCountryNameChangedItems("Locations", updated, logger);
                        await CascadeCountryNameChangedItems("Facilities", updated, logger);
                    }
                    break;
                case nameof(StateLocation):
                    if (HasPropertyChanged(original, updated, "CountryId") || HasPropertyChanged(original, updated, "Name"))
                    {
                        logger.LogInformation($"{updated.Type} cascade change for {original.Name}");
                        await CascadeStateChangedItems("Locations", updated, logger);
                        await CascadeStateChangedItems("Facilities", updated, logger);
                    }
                    break;
                case nameof(RegionLocation):
                    if (HasPropertyChanged(original, updated, "StateId") || HasPropertyChanged(original, updated, "Name"))
                    {
                        logger.LogInformation($"{updated.Type} cascade change for {original.Name}");
                        await CascadeRegionChangedItems("Locations", updated, logger);
                        await CascadeRegionChangedItems("Facilities", updated, logger);
                    }
                    break;
                case nameof(SubRegionLocation):
                    if (HasPropertyChanged(original, updated, "RegionId") || HasPropertyChanged(original, updated, "Name"))
                    {
                        logger.LogInformation($"{updated.Type} cascade change for {original.Name}");
                        await CascadeSubRegionChangedItems("Locations", updated, logger);
                        await CascadeSubRegionChangedItems("Facilities", updated, logger);
                    }
                    break;
                case nameof(BuildingLocation):
                    if (HasPropertyChanged(original, updated, "SubRegionId") || HasPropertyChanged(original, updated, "Name"))
                    {
                        logger.LogInformation($"{updated.Type} cascade change for {original.Name}");
                        await CascadeBuildingChangedItems(updated, logger);
                    }
                    break;
            }
        }

        private async Task CascadeCountryNameChangedItems(string containerName, LocationItem updatedLocation, ILogger logger)
        {
            var sql = $"SELECT * FROM c WHERE c.countryId = '{updatedLocation.Id}'";
            var whereIsActive = "and c.status = 'Active'";
            if (containerName == "Locations")
            {
                sql = $"{sql} {whereIsActive}";
            }

            var list = await GetRows(containerName, sql);

            logger.LogDebug("{updatedLocationType} {updatedLocationName} {listCount} rows to be updated in {containerName} container",
                updatedLocation.Type, updatedLocation.Name, list.Count(), containerName);
            logger.LogDebug(sql);

            foreach (var item in list)
            {
                item["countryName"] = updatedLocation.GetType().GetProperty("Name").GetValue(updatedLocation).ToString();
                string id = item.id;
                await _breezyCosmosService.UpsertItem<dynamic>(containerName, item, id);
            }
        }

        private async Task<IEnumerable<dynamic>> GetRows(string containerName, String sql)
        {
            var queryDef = new QueryDefinition(sql);
            return await _breezyCosmosService.GetList<dynamic>(containerName, queryDef);
        }


        private async Task CascadeStateChangedItems(string containerName, LocationItem updatedLocation, ILogger logger)
        {
            var sql = $"SELECT * FROM c WHERE c.stateId = '{updatedLocation.Id}'";
            var whereIsActive = "and c.status = 'Active'";
            if (containerName == "Locations")
            {
                sql = $"{sql} {whereIsActive}";
            }

            var list = await GetRows(containerName, sql);

            logger.LogDebug("{updatedLocationType} {updatedLocationName} {listCount} rows to be updated in {containerName} container",
                updatedLocation.Type, updatedLocation.Name, list.Count(), containerName);
            logger.LogDebug(sql);

            foreach (var item in list)
            {
                item["stateName"] = updatedLocation.GetType().GetProperty("Name").GetValue(updatedLocation).ToString();
                item["countryId"] = updatedLocation.GetType().GetProperty("CountryId").GetValue(updatedLocation).ToString();
                item["countryName"] = updatedLocation.GetType().GetProperty("CountryName").GetValue(updatedLocation).ToString();
                string id = item.id;
                await _breezyCosmosService.UpsertItem<dynamic>(containerName, item, id);
            }
        }


        private async Task CascadeRegionChangedItems(string containerName, LocationItem updatedLocation, ILogger logger)
        {
            var sql = $"SELECT * FROM c WHERE c.regionId = '{updatedLocation.Id}'";
            var whereIsActive = "and c.status = 'Active'";
            if (containerName == "Locations")
            {
                sql = $"{sql} {whereIsActive}";
            }

            var list = await GetRows(containerName, sql);

            logger.LogDebug("{updatedLocationType} {updatedLocationName} {listCount} rows to be updated in {containerName} container",
                updatedLocation.Type, updatedLocation.Name, list.Count(), containerName);
            logger.LogDebug(sql);

            foreach (var item in list)
            {
                item["regionName"] = updatedLocation.GetType().GetProperty("Name").GetValue(updatedLocation).ToString();
                item["stateId"] = updatedLocation.GetType().GetProperty("StateId").GetValue(updatedLocation).ToString();
                item["stateName"] = updatedLocation.GetType().GetProperty("StateName").GetValue(updatedLocation).ToString();
                item["countryId"] = updatedLocation.GetType().GetProperty("CountryId").GetValue(updatedLocation).ToString();
                item["countryName"] = updatedLocation.GetType().GetProperty("CountryName").GetValue(updatedLocation).ToString();
                string id = item.id;
                await _breezyCosmosService.UpsertItem<dynamic>(containerName, item, id);
            }
        }

        private async Task CascadeSubRegionChangedItems(string containerName, LocationItem updatedLocation, ILogger logger)
        {
            var sql = $"SELECT * FROM c WHERE c.subRegionId = '{updatedLocation.Id}'";
            var whereIsActive = "and c.status = 'Active'";
            if (containerName == "Locations")
            {
                sql = $"{sql} {whereIsActive}";
            }

            var list = await GetRows(containerName, sql);

            logger.LogDebug("{updatedLocation.Type} {updatedLocation.Name} {list.Count()} rows to be updated in {containerName} container",
                updatedLocation.Type, updatedLocation.Name, list.Count());
            logger.LogDebug(sql);

            foreach (var item in list)
            {
                item["subRegionName"] = updatedLocation.GetType().GetProperty("Name").GetValue(updatedLocation).ToString();
                item["regionId"] = updatedLocation.GetType().GetProperty("RegionId").GetValue(updatedLocation).ToString();
                item["regionName"] = updatedLocation.GetType().GetProperty("RegionName").GetValue(updatedLocation).ToString();
                item["stateId"] = updatedLocation.GetType().GetProperty("StateId").GetValue(updatedLocation).ToString();
                item["stateName"] = updatedLocation.GetType().GetProperty("StateName").GetValue(updatedLocation).ToString();
                item["countryId"] = updatedLocation.GetType().GetProperty("CountryId").GetValue(updatedLocation).ToString();
                item["countryName"] = updatedLocation.GetType().GetProperty("CountryName").GetValue(updatedLocation).ToString();


                string id = item.id;
                await _breezyCosmosService.UpsertItem<dynamic>(containerName, item, id);
            }
        }

        private async Task CascadeBuildingChangedItems(LocationItem updatedLocation, ILogger logger)
        {
            var sql = $"SELECT * FROM c WHERE c.buildingId = '{updatedLocation.Id}'";
            var list = await GetRows(breezyContainers.Facilities, sql);

            logger.LogDebug("{updatedLocationType} {updatedLocationName} {listCount} rows to be updated in Facilities container",
                updatedLocation.Type, updatedLocation.Name, list.Count());
            logger.LogDebug(sql);

            foreach (var item in list)
            {
                item["buildingName"] = updatedLocation.GetType().GetProperty("Name").GetValue(updatedLocation).ToString();
                item["countryId"] = updatedLocation.GetType().GetProperty("CountryId").GetValue(updatedLocation).ToString();
                item["countryName"] = updatedLocation.GetType().GetProperty("CountryName").GetValue(updatedLocation).ToString();
                item["stateId"] = updatedLocation.GetType().GetProperty("StateId").GetValue(updatedLocation).ToString();
                item["stateName"] = updatedLocation.GetType().GetProperty("StateName").GetValue(updatedLocation).ToString();
                item["regionId"] = updatedLocation.GetType().GetProperty("RegionId").GetValue(updatedLocation).ToString();
                item["regionName"] = updatedLocation.GetType().GetProperty("RegionName").GetValue(updatedLocation).ToString();
                item["subRegionId"] = updatedLocation.GetType().GetProperty("SubRegionId").GetValue(updatedLocation).ToString();
                item["subRegionName"] = updatedLocation.GetType().GetProperty("SubRegionName").GetValue(updatedLocation).ToString();
                string id = item.id;
                await _breezyCosmosService.UpsertItem<dynamic>(breezyContainers.Facilities, item, id);
            }
        }

        /// <summary>
        /// Deletes a lookup item with the provided id. Returns a 204 response, or 404 if not found.
        /// </summary>
        public async Task<HttpResponseData> LocationDelete<T>(HttpRequestData req, FunctionContext executionContext, string functionName, string id, IDictionary<string, string> pathParams = null) where T : LocationItem
        {
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var locationItem = await _breezyCosmosService.GetItem<T>(breezyContainers.Locations, id, id);
                if (locationItem == null) return await req.NotFoundResponse(logger, $"A location with id '{id}' could not be found");

                var validationError = await ValidateRequest(executionContext, locationItem, pathParams, false);
                if (!string.IsNullOrWhiteSpace(validationError)) return await req.BadRequestResponse(logger, validationError);

                // Cannot delete if there are active children
                var children = await GetActiveChildren(locationItem);
                if (children.Any())
                {
                    return await req.ConflictResponse(logger, $"Could not delete {locationItem.Type} id '{locationItem.Id}' because it is linked to the following active children: {string.Join(", ", children.Select(c => c.Id))}");
                }

                // Cannot delete if there is a linked facility
                var facilities = await GetLinkedFacilities(locationItem);
                if (facilities.Any())
                {
                    return await req.ConflictResponse(logger, $"Could not delete {locationItem.Type} id '{locationItem.Id}' because it is linked to the following facilities: {string.Join(", ", facilities.Select(f => f.Id))}");
                }

                locationItem.Status = "Deleted";
                await _breezyCosmosService.UpsertItem(breezyContainers.Locations, locationItem, id);
                logger.LogInformation("Function {functionName} succeeded!", functionName);

                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Gets a list of facilities linked to the specified location item
        /// </summary>
        private Task<IEnumerable<Facility>> GetLinkedFacilities<T>(T locationItem) where T : LocationItem
        {
            string idField;

            if (locationItem is BuildingLocation)
            {
                idField = "buildingId";
            }
            else if (locationItem is SubRegionLocation)
            {
                idField = "subRegionId";
            }
            else if (locationItem is RegionLocation)
            {
                idField = "regionId";
            }
            else if (locationItem is StateLocation)
            {
                idField = "stateId";
            }
            else if (locationItem is CountryLocation)
            {
                idField = "countryId";
            }
            else
            {
                return Task.FromResult(Enumerable.Empty<Facility>());
            }

            var queryDef = new QueryDefinition($"SELECT * FROM f WHERE f.{idField} = @id");
            queryDef.WithParameter("@id", locationItem.Id);
            return _breezyCosmosService.GetList<Facility>(breezyContainers.Facilities, queryDef);
        }

        /// <summary>
        /// Returns a list of active child location items linked to the specified parent item
        /// </summary>
        private Task<IEnumerable<T>> GetActiveChildren<T>(T locationItem) where T : LocationItem
        {
            if (locationItem is SubRegionLocation)
            {
                var subRegion = locationItem as SubRegionLocation;
                return ActiveLocations<T>("subRegionId", subRegion.Id, LocationType.building);
            }

            if (locationItem is RegionLocation)
            {
                var region = locationItem as RegionLocation;
                return ActiveLocations<T>("regionId", region.Id, LocationType.subregion);
            }

            if (locationItem is StateLocation)
            {
                var state = locationItem as StateLocation;
                return ActiveLocations<T>("stateId", state.Id, LocationType.region);
            }

            if (locationItem is CountryLocation)
            {
                var country = locationItem as CountryLocation;
                return ActiveLocations<T>("countryId", country.Id, LocationType.state);
            }

            return Task.FromResult(Enumerable.Empty<T>());
        }

        /// <summary>
        /// Returns a list of locations of the specified id and type with active status
        /// </summary>
        private Task<IEnumerable<T>> ActiveLocations<T>(string idName, string idValue, LocationType type) where T : LocationItem
        {
            var sql = $"SELECT * FROM c WHERE c.status = 'Active' AND c.type = '{type}' AND c.{idName} = '{idValue}'";
            return _breezyCosmosService.GetList<T>(breezyContainers.Locations, new QueryDefinition(sql));
        }

        /// <summary>
        /// Validates the provided location item against the path parameters, and fetches the location names for each location id.
        /// </summary>
        /// <returns>An error message, or null if validation passes</returns>
        private async Task<string> ValidateRequest(FunctionContext executionContext, LocationItem locationItem, IDictionary<string, string> pathParams, bool isInsert)
        {
            if (locationItem is BuildingLocation buildingLocation)
            {
                if (!pathParams.ContainsKey("countryId")) return "Country id missing from path parameters";
                if (pathParams["countryId"] != buildingLocation.CountryId) return "Path parameter 'countryId' does not match request body";
                if (!pathParams.ContainsKey("stateId")) return "State id missing from path parameters";
                if (pathParams["stateId"] != buildingLocation.StateId) return "Path parameter 'stateId' does not match request body";
                if (!pathParams.ContainsKey("regionId")) return "Region id missing from path parameters";
                if (pathParams["regionId"] != buildingLocation.RegionId) return "Path parameter 'regionId' does not match request body";
                if (!pathParams.ContainsKey("subRegionId")) return "Subregion id missing from path parameters";
                if (pathParams["subRegionId"] != buildingLocation.SubRegionId) return "Path parameter 'subRegionId' does not match request body";
                if (!isInsert)
                {
                    if (!pathParams.ContainsKey("id")) return "Building id missing from path parameters";
                    if (pathParams["id"] != buildingLocation.Id) return "Path parameter 'buildingId' does not match request body";
                }
                buildingLocation.CountryName = await GetCountryName(executionContext, buildingLocation.CountryId);
                if (string.IsNullOrWhiteSpace(buildingLocation.CountryName)) return $"Could not find active Country location with id '{buildingLocation.CountryId}'";
                buildingLocation.StateName = await GetStateName(executionContext, buildingLocation.StateId);
                if (string.IsNullOrWhiteSpace(buildingLocation.StateName)) return $"Could not find active State location with id '{buildingLocation.StateId}'";
                buildingLocation.RegionName = await GetRegionName(executionContext, buildingLocation.RegionId);
                if (string.IsNullOrWhiteSpace(buildingLocation.RegionName)) return $"Could not find active Region location with id '{buildingLocation.RegionId}'";
                buildingLocation.SubRegionName = await GetSubRegionName(executionContext, buildingLocation.SubRegionId);
                if (string.IsNullOrWhiteSpace(buildingLocation.SubRegionName)) return $"Could not find active Subregion location with id '{buildingLocation.SubRegionId}'";
            }
            else if (locationItem is SubRegionLocation subRegionLocation)
            {
                if (!pathParams.ContainsKey("countryId")) return "Country id missing from path parameters";
                if (pathParams["countryId"] != subRegionLocation.CountryId) return "Path parameter 'countryId' does not match request body";
                if (!pathParams.ContainsKey("stateId")) return "State id missing from path parameters";
                if (pathParams["stateId"] != subRegionLocation.StateId) return "Path parameter 'stateId' does not match request body";
                if (!pathParams.ContainsKey("regionId")) return "Region id missing from path parameters";
                if (pathParams["regionId"] != subRegionLocation.RegionId) return "Path parameter 'regionId' does not match request body";
                if (!isInsert)
                {
                    if (!pathParams.ContainsKey("id")) return "Subregion id missing from path parameters";
                    if (pathParams["id"] != subRegionLocation.Id) return "Path parameter 'subRegionId' does not match request body";
                }
                subRegionLocation.CountryName = await GetCountryName(executionContext, subRegionLocation.CountryId);
                if (string.IsNullOrWhiteSpace(subRegionLocation.CountryName)) return $"Could not find active Country location with id '{subRegionLocation.CountryId}'";
                subRegionLocation.StateName = await GetStateName(executionContext, subRegionLocation.StateId);
                if (string.IsNullOrWhiteSpace(subRegionLocation.StateName)) return $"Could not find active State location with id '{subRegionLocation.StateId}'";
                subRegionLocation.RegionName = await GetRegionName(executionContext, subRegionLocation.RegionId);
                if (string.IsNullOrWhiteSpace(subRegionLocation.RegionName)) return $"Could not find active Region location with id '{subRegionLocation.RegionId}'";
            }
            else if (locationItem is RegionLocation regionLocation)
            {
                if (!pathParams.ContainsKey("countryId")) return "Country id missing from path parameters";
                if (pathParams["countryId"] != regionLocation.CountryId) return "Path parameter 'countryId' does not match request body";
                if (!pathParams.ContainsKey("stateId")) return "State id missing from path parameters";
                if (pathParams["stateId"] != regionLocation.StateId) return "Path parameter 'stateId' does not match request body";
                if (!isInsert)
                {
                    if (!pathParams.ContainsKey("id")) return "Region id missing from path parameters";
                    if (pathParams["id"] != regionLocation.Id) return "Path parameter 'regionId' does not match request body";
                }
                regionLocation.CountryName = await GetCountryName(executionContext, regionLocation.CountryId);
                if (string.IsNullOrWhiteSpace(regionLocation.CountryName)) return $"Could not find active Country location with id '{regionLocation.CountryId}'";
                regionLocation.StateName = await GetStateName(executionContext, regionLocation.StateId);
                if (string.IsNullOrWhiteSpace(regionLocation.StateName)) return $"Could not find active State location with id '{regionLocation.StateId}'";
            }
            else if (locationItem is StateLocation stateLocation)
            {
                if (!pathParams.ContainsKey("countryId")) return "Country id missing from path parameters";
                if (pathParams["countryId"] != stateLocation.CountryId) return "Path parameter 'countryId' does not match request body";
                if (!isInsert)
                {
                    if (!pathParams.ContainsKey("id")) return "State id missing from path parameters";
                    if (pathParams["id"] != stateLocation.Id) return "Path parameter 'stateId' does not match request body";
                }
                stateLocation.CountryName = await GetCountryName(executionContext, stateLocation.CountryId);
                if (string.IsNullOrWhiteSpace(stateLocation.CountryName)) return $"Could not find active Country location with id '{stateLocation.CountryId}'";
            }
            else if (locationItem is CountryLocation countryLocation)
            {
                if (!isInsert)
                {
                    if (!pathParams.ContainsKey("id")) return "Country id missing from path parameters";
                    if (pathParams["id"] != countryLocation.Id) return "Path parameter 'countryId' does not match request body";
                }
            }

            return null; // Validation passed
        }

        private async Task<string> GetCountryName(FunctionContext executionContext, string countryId)
        {
            try
            {
                var location = await _breezyCosmosService.GetItem<CountryLocation>(breezyContainers.Locations, countryId, countryId);
                if (location is null || location.Status != "Active" || location.Type != LocationType.country)
                {
                    var logger = executionContext.GetLogger(nameof(GetCountryName));
                    logger.LogError("Could not find active Country location with id '{countryId}'", countryId);
                    return null;
                }
                return location.Name;
            }
            catch (Exception ex)
            {
                var logger = executionContext.GetLogger(nameof(GetCountryName));
                logger.LogError(ex, "Could not find location with id '{countryId}'", countryId);
                return null;
            }
        }

        private async Task<string> GetStateName(FunctionContext executionContext, string stateId)
        {
            try
            {
                var location = await _breezyCosmosService.GetItem<StateLocation>(breezyContainers.Locations, stateId, stateId);
                if (location is null || location.Status != "Active" || location.Type != LocationType.state)
                {
                    var logger = executionContext.GetLogger(nameof(GetStateName));
                    logger.LogError("Could not find active State location with id '{stateId}'", stateId);
                    return null;
                }
                return location.Name;
            }
            catch (Exception ex)
            {
                var logger = executionContext.GetLogger(nameof(GetStateName));
                logger.LogError(ex, "Could not find location with id '{stateId}'", stateId);
                return null;
            }
        }

        private async Task<string> GetRegionName(FunctionContext executionContext, string regionId)
        {
            try
            {
                var location = await _breezyCosmosService.GetItem<RegionLocation>(breezyContainers.Locations, regionId, regionId);
                if (location is null || location.Status != "Active" || location.Type != LocationType.region)
                {
                    var logger = executionContext.GetLogger(nameof(GetRegionName));
                    logger.LogError("Could not find active Region location with id '{regionId}'", regionId);
                    return null;
                }
                return location.Name;
            }
            catch (Exception ex)
            {
                var logger = executionContext.GetLogger(nameof(GetRegionName));
                logger.LogError(ex, "Could not find location with id '{regionId}'", regionId);
                return null;
            }
        }

        private async Task<string> GetSubRegionName(FunctionContext executionContext, string subRegionId)
        {
            try
            {
                var location = await _breezyCosmosService.GetItem<SubRegionLocation>(breezyContainers.Locations, subRegionId, subRegionId);
                if (location is null || location.Status != "Active" || location.Type != LocationType.subregion)
                {
                    var logger = executionContext.GetLogger(nameof(GetSubRegionName));
                    logger.LogError("Could not find active Subregion location with id '{subRegionId}'", subRegionId);
                    return null;
                }
                return location.Name;
            }
            catch (Exception ex)
            {
                var logger = executionContext.GetLogger(nameof(GetSubRegionName));
                logger.LogError(ex, "Could not find location with id '{subRegionId}'", subRegionId);
                return null;
            }
        }

        #endregion

        #region Models

        [JsonConverter(typeof(StringEnumConverter))]
        public enum LocationType
        {
            [EnumMember(Value = "unknown")]
            unknown,
            [EnumMember(Value = "country")]
            country,
            [EnumMember(Value = "state")]
            state,
            [EnumMember(Value = "region")]
            region,
            [EnumMember(Value = "subregion")]
            subregion,
            [EnumMember(Value = "building")]
            building
        }

        public abstract class LocationItemParams : LookupBase
        {
            [OpenApiProperty(Description = "The unique id of the location")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("id")]
            [JsonRequired]
            public string Id { get; set; }
            [OpenApiProperty(Description = "The name of the location")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("name")]
            [JsonRequired]
            public string Name { get; set; }
        }

        public abstract class LocationItem : LookupBase
        {
            [OpenApiProperty(Description = "The unique id of the location")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("id")]
            [JsonRequired]
            public string Id { get; set; }
            [OpenApiProperty(Description = "The name of the location")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("name")]
            [JsonRequired]
            public string Name { get; set; }
            [OpenApiProperty(Description = "The current status of the location")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("status")]
            [JsonRequired]
            public string Status { get; set; }
            [OpenApiProperty(Description = "The type of the location")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("type")]
            [JsonRequired]
            public LocationType Type { get; set; }
            [OpenApiProperty(Description = "The list of related documents", Nullable = true)]
            [JsonProperty("activeRelations")]
            public ActiveRelationItem[] ActiveRelations { get; set; }
        }

        public class CountryLocationParams : LocationItemParams
        {
        }

        public class CountryLocation : LocationItem
        {
        }

        public class StateLocationParams : CountryLocationParams
        {
            [OpenApiProperty(Description = "The unique id of the country")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("countryId")]
            [JsonRequired]
            public string CountryId { get; set; }
            [OpenApiProperty(Description = "The name of the country")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("countryName")]
            public string CountryName { get; set; }
        }

        public class StateLocation : CountryLocation
        {
            [OpenApiProperty(Description = "The unique id of the country")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("countryId")]
            [JsonRequired]
            public string CountryId { get; set; }
            [OpenApiProperty(Description = "The name of the country")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("countryName")]
            public string CountryName { get; set; }
        }

        public class RegionLocationParams : StateLocationParams
        {
            [OpenApiProperty(Description = "The abbreviated name of the region", Nullable = true)]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("nameAbbreviated")]
            public string NameAbbreviated { get; set; }

            [OpenApiProperty(Description = "The unique id of the state")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("stateId")]
            [JsonRequired]
            public string StateId { get; set; }
            [OpenApiProperty(Description = "The name of the state")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("stateName")]
            public string StateName { get; set; }
        }

        public class RegionLocation : StateLocation
        {
            [OpenApiProperty(Description = "The abbreviated name of the region", Nullable = true)]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("nameAbbreviated")]
            public string NameAbbreviated { get; set; }

            [OpenApiProperty(Description = "The unique id of the state")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("stateId")]
            [JsonRequired]
            public string StateId { get; set; }
            [OpenApiProperty(Description = "The name of the state")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("stateName")]
            public string StateName { get; set; }
        }

        public class SubRegionLocationParams : RegionLocationParams
        {
            [OpenApiProperty(Description = "The unique id of the region")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("regionId")]
            [JsonRequired]
            public string RegionId { get; set; }
            [OpenApiProperty(Description = "The name of the region")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("regionName")]
            public string RegionName { get; set; }
        }

        public class SubRegionLocation : RegionLocation
        {
            [OpenApiProperty(Description = "The unique id of the region")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("regionId")]
            [JsonRequired]
            public string RegionId { get; set; }
            [OpenApiProperty(Description = "The name of the region")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("regionName")]
            public string RegionName { get; set; }
        }

        public class BuildingLocationParams : SubRegionLocationParams
        {
            [OpenApiProperty(Description = "The unique id of the subregion")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("subRegionId")]
            [JsonRequired]
            public string SubRegionId { get; set; }
            [OpenApiProperty(Description = "The name of the subregion")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("subRegionName")]
            public string SubRegionName { get; set; }
        }

        public class BuildingLocation : SubRegionLocation
        {
            [OpenApiProperty(Description = "The unique id of the subregion")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("subRegionId")]
            [JsonRequired]
            public string SubRegionId { get; set; }
            [OpenApiProperty(Description = "The name of the subregion")]
            [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
            [JsonProperty("subRegionName")]
            public string SubRegionName { get; set; }
        }

        #endregion
    }
}
