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
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    //TODO: Need refactoring
    public class LookupService
    {
        private readonly IBreezyCosmosService _breezyCosmosService;
        private readonly IAuthorisationService _authService;

        public LookupService(IBreezyCosmosService breezyCosmosService, IAuthorisationService authService)
        {
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        /// <summary>
        /// Returns a list of all lookup items with status of Active.
        /// </summary>
        public async Task<HttpResponseData> LookupRetrieveAll(HttpRequestData req, FunctionContext executionContext, string containerName, string functionName, string queryText = null, IDictionary<string, object> queryParams = null)
        {
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var queryDef = new QueryDefinition(queryText ?? "SELECT * FROM c WHERE c.status = 'Active'");
                if (queryParams != null)
                {
                    foreach (var kvp in queryParams) queryDef.WithParameter(kvp.Key.StartsWith("@") ? kvp.Key : "@" + kvp.Key, kvp.Value);
                }
                var lookupItems = await _breezyCosmosService.GetList<LookupItem>(containerName, queryDef);

                logger.LogInformation("Function {functionName} succeeded!", functionName);

                var retrievedResponse = req.CreateResponse(HttpStatusCode.OK);
                var obj = Newtonsoft.Json.Linq.JArray.FromObject(lookupItems.ToArray());
                await retrievedResponse.WriteStringAsync(obj.ToString());
                return retrievedResponse;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Returns a lookup item with the provided id, or 404 if not found.
        /// </summary>
        public async Task<HttpResponseData> LookupRetrieve(HttpRequestData req, FunctionContext executionContext, string containerName, string functionName, string id)
        {
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var lookupItem = await _breezyCosmosService.GetItem<LookupItem>(containerName, id, id);
                if (lookupItem == null) return await req.NotFoundResponse(logger, $"A lookup item with id '{id}' could not be found");
                lookupItem.ActiveRelations = (await GetActiveRelations(containerName, id)).ToArray();
                logger.LogInformation("Function {functionName} succeeded!", functionName);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(lookupItem.ToJsonString());
                return response;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Gets a summary of documents that are related to a lookup item.
        /// </summary>
        private async Task<IEnumerable<ActiveRelationItem>> GetActiveRelations(string containerName, string id)
        {
            var relationItems = new List<ActiveRelationItem>();

            switch (containerName)
            {
                case "Titles":
                    relationItems.Add(await GetActiveRelations("User", "Users", "titleId", id));
                    break;
                case "PersonnelRoles":
                    relationItems.Add(await GetActiveRelations("User", "Users", "roleId", id));
                    break;
                case "Departments":
                    relationItems.Add(await GetActiveRelations("User", "Users", "departmentId", id));
                    relationItems.Add(await GetActiveRelations("Calendar", "Calendars", "departmentId", id));
                    break;
            }

            return relationItems;
        }

        /// <summary>
        /// Gets a count of documents in the given container that are related to the lookup item with the provided id.
        /// </summary>
        private async Task<ActiveRelationItem> GetActiveRelations(string relationName, string relationContainer, string relationField, string id)
        {
            var sql = $"SELECT VALUE COUNT(1) FROM c WHERE c.{relationField} = @id";
            if (relationContainer == "Users") sql += " AND c.activeFlag = true";
            var queryDef = new QueryDefinition(sql).WithParameter("@id", id);
            return new ActiveRelationItem() { Name = relationName, ActiveCount = await _breezyCosmosService.GetItem<int>(relationContainer, queryDef) };
        }

        /// <summary>
        /// Inserts a new lookup item. Returns a 201 response or 409 if the id already exists.
        /// </summary>
        public async Task<HttpResponseData> LookupInsert(HttpRequestData req, FunctionContext executionContext, string containerName, string functionName)
        {
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            LookupItem lookupItem;
            try
            {
                lookupItem = await req.ReadFromJsonAsync<LookupItem>();
            }
            catch (Exception ex)
            {
                return await req.BadRequestResponse(logger, ex.Message);
            }
            var validationError = lookupItem.Validate();
            if (!string.IsNullOrWhiteSpace(validationError)) return await req.BadRequestResponse(logger, validationError);

            try
            {
                var existingItem = await _breezyCosmosService.GetItem<LookupItem>(containerName, lookupItem.Id, lookupItem.Id);
                if (existingItem != null)
                {
                    return await req.ConflictResponse(logger, containerName, lookupItem.Id);
                }

                var createResponse = await _breezyCosmosService.CreateItem(containerName, lookupItem, null);
                logger.LogInformation("Function {functionName} succeeded!", functionName);

                var createdResponse = req.CreateResponse(HttpStatusCode.Created);
                await createdResponse.WriteStringAsync(createResponse.Resource.ToJsonString());
                return createdResponse;
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Updates an existing lookup item with the provided id. Returns a 200 response, or 404 if not found.
        /// </summary>
        public async Task<HttpResponseData> LookupUpdate(HttpRequestData req, FunctionContext executionContext, string containerName, string functionName, string id)
        {
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            LookupItem lookupItem;
            try
            {
                lookupItem = await req.ReadFromJsonAsync<LookupItem>();
            }
            catch (Exception ex)
            {
                return await req.BadRequestResponse(logger, ex.Message);
            }
            var validationError = lookupItem.Validate();
            if (!string.IsNullOrWhiteSpace(validationError)) return await req.BadRequestResponse(logger, validationError);

            try
            {
                var existingItem = await _breezyCosmosService.GetItem<LookupItem>(containerName, id, id);
                if (existingItem == null) return await req.NotFoundResponse(logger, $"A lookup item with id '{id}' could not be found");

                // Update the lookup item
                var upsertResponse = await _breezyCosmosService.UpsertItem(containerName, lookupItem, id);

                // Update any linked documents, if required
                if (containerName == "Titles" && lookupItem.Name != existingItem.Name)
                {
                    var updatecount = await UserTitleUpdate(upsertResponse.Resource);
                    logger.LogInformation("Updated {updatecount} user documents which were linked to updated title {lookupItemId}",
                        updatecount, lookupItem.Id);
                }
                else if (containerName == "PersonnelRoles" && lookupItem.Name != existingItem.Name)
                {
                    var updatecount = await UserRoleUpdate(upsertResponse.Resource);
                    logger.LogInformation("Updated {updatecount} user documents which were linked to updated personnel role {lookupItemId}",
                        updatecount, lookupItem.Id);
                }
                else if (containerName == "Departments" && lookupItem.Name != existingItem.Name)
                {
                    var updatecount = await UserDepartmentUpdate(upsertResponse.Resource);
                    logger.LogInformation("Updated {updatecount} user documents which were linked to updated department {lookupItemId}",
                        updatecount, lookupItem.Id);
                    updatecount = await CalendarDepartmentUpdate(upsertResponse.Resource);
                    logger.LogInformation("Updated {updatecount} calendar documents which were linked to updated department {lookupItemId}",
                        updatecount, lookupItem.Id);
                }

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

        /// <summary>
        /// Updates any user linked to the provided title with the new title name
        /// </summary>
        private async Task<int> UserTitleUpdate(LookupItem item)
        {
            var queryDef = new QueryDefinition("SELECT * FROM u WHERE u.titleId = @titleId")
                .WithParameter("@titleId", item.Id);
            var users = await _breezyCosmosService.GetList<User>(breezyContainers.Users, queryDef);

            var count = 0;
            foreach (var user in users)
            {
                user.TitleName = item.Name;
                await _breezyCosmosService.UpsertItem(breezyContainers.Users, user, user.Email);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Updates any user linked to the provided role with the new role name
        /// </summary>
        private async Task<int> UserRoleUpdate(LookupItem item)
        {
            var queryDef = new QueryDefinition("SELECT * FROM u WHERE u.roleId = @roleId")
                .WithParameter("@roleId", item.Id);
            var users = await _breezyCosmosService.GetList<User>(breezyContainers.Users, queryDef);

            var count = 0;
            foreach (var user in users)
            {
                user.RoleName = item.Name;
                await _breezyCosmosService.UpsertItem(breezyContainers.Users, user, user.Email);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Updates any user linked to the provided department with the new department name
        /// </summary>
        private async Task<int> UserDepartmentUpdate(LookupItem item)
        {
            var queryDef = new QueryDefinition("SELECT * FROM u WHERE u.departmentId = @departmentId")
                .WithParameter("@departmentId", item.Id);
            var users = await _breezyCosmosService.GetList<User>(breezyContainers.Users, queryDef);

            var count = 0;
            foreach (var user in users)
            {
                user.DepartmentName = item.Name;
                await _breezyCosmosService.UpsertItem(breezyContainers.Users, user, user.Email);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Updates any calendar linked to the provided department with the new department name
        /// </summary>
        private async Task<int> CalendarDepartmentUpdate(LookupItem item)
        {
            var queryDef = new QueryDefinition("SELECT * FROM c WHERE c.departmentId = @departmentId")
                .WithParameter("@departmentId", item.Id);
            var calendars = await _breezyCosmosService.GetList<Calendar>(breezyContainers.Calendars, queryDef);

            var count = 0;
            foreach (var calendar in calendars)
            {
                calendar.DepartmentName = item.Name;
                await _breezyCosmosService.UpsertItem(breezyContainers.Calendars, calendar, calendar.Id);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Deletes a lookup item with the provided id. Returns a 204 response, or 404 if not found.
        /// </summary>
        public async Task<HttpResponseData> LookupDelete(HttpRequestData req, FunctionContext executionContext, string containerName, string functionName, string id)
        {
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                var lookupItem = await _breezyCosmosService.GetItem<LookupItem>(containerName, id, id);
                if (lookupItem == null) return await req.NotFoundResponse(logger, $"A lookup item with id '{id}' could not be found");

                // Check if there are any linked documents
                if (containerName == "Titles")
                {
                    var users = await GetLinkedUsers("titleId", id);
                    if (users.Any())
                    {
                        return await req.ConflictResponse(logger, $"Could not delete title id '{id}' because it is linked to the following users: {string.Join(", ", users.Select(f => f.Email))}");
                    }
                }
                else if (containerName == "PersonnelRoles")
                {
                    var users = await GetLinkedUsers("roleId", id);
                    if (users.Any())
                    {
                        return await req.ConflictResponse(logger, $"Could not delete personnel role id '{id}' because it is linked to the following users: {string.Join(", ", users.Select(f => f.Email))}");
                    }
                }
                else if (containerName == "Departments")
                {
                    var users = await GetLinkedUsers("departmentId", id);
                    if (users.Any())
                    {
                        return await req.ConflictResponse(logger, $"Could not delete department id '{id}' because it is linked to the following users: {string.Join(", ", users.Select(f => f.Email))}");
                    }
                    var calendars = await GetLinkedCalendars(id);
                    if (calendars.Any())
                    {
                        return await req.ConflictResponse(logger, $"Could not delete department id '{id}' because it is linked to the following calendars: {string.Join(", ", calendars.Select(c => c.ExternalCalendarId))}");
                    }
                }

                lookupItem.Status = "Deleted";
                await _breezyCosmosService.UpsertItem(containerName, lookupItem, id);
                logger.LogInformation("Function {functionName} succeeded!", functionName);

                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        /// <summary>
        /// Gets a list of active users linked to the specified lookup id via the provided id field
        /// </summary>
        private Task<IEnumerable<User>> GetLinkedUsers(string idField, string lookupId)
        {
            var queryDef = new QueryDefinition($"SELECT * FROM u WHERE u.{idField} = @id AND u.activeFlag = true")
                .WithParameter("@id", lookupId);
            return _breezyCosmosService.GetList<User>(breezyContainers.Users, queryDef);
        }

        /// <summary>
        /// Gets a list of calendars linked to the specified department
        /// </summary>
        private Task<IEnumerable<Calendar>> GetLinkedCalendars(string departmentId)
        {
            var queryDef = new QueryDefinition($"SELECT * FROM c WHERE c.departmentId = @departmentId")
                .WithParameter("@departmentId", departmentId);
            return _breezyCosmosService.GetList<Calendar>(breezyContainers.Calendars, queryDef);
        }
    }

    public abstract class LookupBase
    {
        /// <summary>
        /// Validates the lookup object and constructs an error message based on any validation errors found.
        /// </summary>
        public string Validate()
        {
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(this, new ValidationContext(this), validationResults);
            return isValid ? null : string.Join('\n', validationResults.Select(r => r.ErrorMessage));
        }

        /// <summary>
        /// Converts the lookup object to a JSON object and stringifies it.
        /// </summary>
        public string ToJsonString()
        {
            return Newtonsoft.Json.Linq.JObject.FromObject(this, JsonSerializer.CreateDefault(new JsonSerializerSettings { Converters = { new StringEnumConverter() } })).ToString();
        }

        /// <summary>
        /// Converts the array of lookup objects to a JSON object and stringifies it.
        /// </summary>
        public static string ToJsonArrayString<T>(IEnumerable<T> items) where T : LookupBase
        {
            return Newtonsoft.Json.Linq.JArray.FromObject(items.ToArray(), JsonSerializer.CreateDefault(new JsonSerializerSettings { Converters = { new StringEnumConverter() } })).ToString();
        }
    }

    public class LookupItem : LookupBase
    {
        [OpenApiProperty(Description = "The unique id of the lookup item")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("id")]
        [JsonRequired]
        public string Id { get; set; }
        [OpenApiProperty(Description = "The name of the lookup item")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("name")]
        [JsonRequired]
        public string Name { get; set; }
        [OpenApiProperty(Description = "The current status of the lookup item")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("status")]
        [JsonRequired]
        public string Status { get; set; }
        [OpenApiProperty(Description = "The list of related documents", Nullable = true)]
        [JsonProperty("activeRelations")]
        public ActiveRelationItem[] ActiveRelations { get; set; }
    }

    public class ActiveRelationItem
    {
        [OpenApiProperty(Description = "The name of the related document")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("name")]
        [JsonRequired]
        public string Name { get; set; }
        [OpenApiProperty(Description = "The count of active related documents")]
        [JsonProperty("activeCount")]
        [JsonRequired]
        public int ActiveCount { get; set; }

    }
}