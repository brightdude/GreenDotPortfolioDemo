using Ardalis.GuardClauses;
using Breezy.Muticaster.TenantSettings;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public partial class User : UserCreateUpdateParams
    {
        private readonly IOptionsMonitor<MsTeamsOptions> _options;
        private readonly IOptionsMonitor<CredentialOptions> _credentialOptions;
        private readonly IBreezyCosmosService _breezyCosmosService;
        private readonly ICalendarService _calendarService;
        private readonly IGraphTeamsService _graphTeamsService;
        private readonly IGraphTeamsMembersService _graphTeamsMembersService;
        private readonly IGraphTeamsChannelsService _graphTeamsChannelsService;
        private readonly IGraphTeamsChannelsMembersService _graphTeamsChannelsMembersService;
        private readonly IGraphUsersService _graphUsersService;
        private readonly TenantSettingsService _tenantSettingsService; //TODO: This need refactoring   
        private readonly IAuthorisationService _authService;

        //TODO: Move data out off function class
        public User()
        {
        }

        [ActivatorUtilitiesConstructor]
        public User(
            IOptionsMonitor<MsTeamsOptions> options,
            IOptionsMonitor<CredentialOptions> credentialOptions,
            IBreezyCosmosService breezyCosmosService,
            ICalendarService calendarService,
            IGraphTeamsService graphTeamsService,
            IGraphTeamsMembersService graphTeamsMembersService,
            IGraphTeamsChannelsService graphTeamsChannelsService,
            IGraphTeamsChannelsMembersService graphTeamsChannelsMembersService,
            IGraphUsersService graphUsersService,
            IAuthorisationService authService)
        {
            _options = Guard.Against.Null(options, nameof(options));
            _credentialOptions = Guard.Against.Null(credentialOptions, nameof(credentialOptions));
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _calendarService = Guard.Against.Null(calendarService, nameof(calendarService));
            _graphTeamsService = Guard.Against.Null(graphTeamsService, nameof(graphTeamsService));
            _graphTeamsMembersService = Guard.Against.Null(graphTeamsMembersService, nameof(graphTeamsMembersService));
            _graphTeamsChannelsService = Guard.Against.Null(graphTeamsChannelsService, nameof(graphTeamsChannelsService));
            _graphTeamsChannelsMembersService = Guard.Against.Null(graphTeamsChannelsMembersService, nameof(graphTeamsChannelsMembersService));
            _graphUsersService = Guard.Against.Null(graphUsersService, nameof(graphUsersService));
            _tenantSettingsService = new TenantSettingsService(breezyCosmosService);
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        /// <summary>
        /// Creates a new ms user object for the provided user via invitation.
        /// </summary>
        private async Task<Microsoft.Graph.Invitation> MsUserCreate(User user, ILogger logger)
        {
            var settings = await _tenantSettingsService.GetTenantSettings();

            // Create invitation for the provided user
            var invitation = await _graphUsersService.CreateInvitation(
                AuthenticationType.GraphService,
                user.Email,
                _options.CurrentValue.GeneralChannelId,
                _options.CurrentValue.EveryoneTeamId,
                settings.TenantId);

            // Update the newly created ms user
            await _graphUsersService.UserUpdate(AuthenticationType.GraphService, invitation.InvitedUser.Id, new Microsoft.Graph.User
            {
                Department = user.DepartmentName,
                DisplayName = user.DisplayName,
                GivenName = user.FirstName,
                Surname = user.LastName,
                UsageLocation = settings.DefaultUsageLocation
            });

            // TODO this is going away
            // Add user to the Everyone team
            await _graphTeamsMembersService.TeamMemberCreate(
                AuthenticationType.ScheduledEventService,
                _options.CurrentValue.EveryoneTeamId,
                invitation.InvitedUser.Id);

            // Email setup instructions to user
            await _graphUsersService.SendMail(
                AuthenticationType.GraphService,
                _credentialOptions.CurrentValue.ScheduledEventService.Username,
                user.Email,
                "Virtual Justice Set-up Instructions",
                ComposeEmailBody(invitation.InviteRedeemUrl));

            return invitation;
        }

        // TODO move this to tenant settings
        private static string ComposeEmailBody(string inviteRedeemUrl)
        {
            return @"
                <!doctype html>
                <html>
                <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <meta http-equiv='X-UA-Compatible' content='IE=edge'>
                <title>EmailTemplate</title>
                <style type='text/css'>
                html, body {
	                margin: 0 !important;
	                padding: 0 !important;
	                height: 100% !important;
	                width: 100% !important;
                }
                * {
	                -ms-text-size-adjust: 100%;
	                -webkit-text-size-adjust: 100%;
                }
                .ExternalClass {
	                width: 100%;
                }
                div[style*='margin: 16px 0'] {
	                margin: 0 !important;
                }
                table, td {
	                mso-table-lspace: 0pt !important;
	                mso-table-rspace: 0pt !important;
                }
                table {
	                border-spacing: 0 !important;
	                border-collapse: collapse !important;
	                table-layout: fixed !important;
	                margin: 0 auto !important;
                }
                table table table {
	                table-layout: auto;
                }
                img {
	                -ms-interpolation-mode: bicubic;
                }
                .yshortcuts a {
	                border-bottom: none !important;
                }
                a[x-apple-data-detectors] {
	                color: inherit !important;
                }
                </style>
                <style type='text/css'>
                .button-td, .button-a {
	                transition: all 100ms ease-in;
                }
                .button-td:hover, .button-a:hover {
	                background: #555555 !important;
	                border-color: #555555 !important;
                }
                </style>
                </head>
                <body width='100%' height='100%' bgcolor='#e0e0e0' style='margin: 0;' yahoo='yahoo'>
                <table cellpadding='0' cellspacing='0' border='0' height='100%' width='100%' bgcolor='#e0e0e0' style='border-collapse:collapse;'>
                  <tr>
                    <td><center style='width: 100%;'>
                        <div style='max-width: 600px;'> 
                          <!--[if (gte mso 9)|(IE)]>
                            <table cellspacing='0' cellpadding='0' border='0' width='600' align='center'>
                            <tr>
                            <td>
                            <![endif]--> 
                          <table cellspacing='0' cellpadding='0' border='0' align='center' width='100%' style='max-width: 600px;'>
                            <tr>
                              <td style='padding: 20px 0; text-align: center'>&nbsp;</td>
                            </tr>
                          </table>
                          <table cellspacing='0' cellpadding='0' border='0' align='center' bgcolor='#ffffff' width='100%' style='max-width: 600px;'>
                            <tr>
                              <td class='full-width-image' align='center'>
                                <img src='data:image/png;base64,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' 
                                width='600' alt='alt_text' border='0' style='width: 100%; max-width: 600px; height ='150';'>
                              </td>
                            </tr>
                            <tr>
                              <td align='center' style='font-size: 22px; font-weight: 300; line-height: 2.5em; color: #000000; font-family: sans-serif; padding: 10px'>Virtual Justice Set-up Instructions </td>
                            </tr>
                            <tr>
                              <td><table cellspacing='0' cellpadding='0' border='0' width='100%'>
                                  <tr>
                                    <td style='padding: 20px; font-family: sans-serif; font-size: 15px; mso-height-rule: exactly; line-height: 20px; color: #555555;'> 
						                <p>You have been identified by your jurisdiction as a user of the Virtual Justice platform, which is used to conduct remote focus proceedings. In order to access the platform, you must take the following steps:<br><br>
							                1. Accept the invitation via the link below
							                <br>
							                2. Sign in to Microsoft Teams
							                <br>
							                3. Launch Microsoft Teams
							                <br><br><br>
                                      <table cellspacing='10' cellpadding='10' border='0' align='center' style='margin: auto;'>
                                        <tr>
                                          <td style='border-radius: 3px; background: #222222; text-align: center;' class='button-td'><a href='[INSERT REDEEM URL HERE]' style='background: #222222; border: 15px solid #222222; padding: 0 10px;color: #ffffff; font-family: sans-serif; font-size: 13px; line-height: 1.1; text-align: center; text-decoration: none; display: block; border-radius: 3px; font-weight: bold;' class='button-a'> 
                                            <!--[if mso]>&nbsp;&nbsp;&nbsp;&nbsp;<![endif]-->ACCEPT INVITATION<!--[if mso]>&nbsp;&nbsp;&nbsp;&nbsp;<![endif]--> 
                                            </a></td>
                                        </tr>
                                      </table>
                                      <br>
  				                </td>
                                  </tr>
                                </table></td>
                            </tr>
                            <tr>
                              <td bgcolor='#ffffff' align='center' height='100%' valign='top' width='100%'><!--[if mso]>
                                        <table cellspacing='0' cellpadding='0' border='0' align='center' width='560'>
                                        <tr>
                                        <td align='center' valign='top' width='560'>
                                        <![endif]-->
                                <table border='0' cellpadding='0' cellspacing='0' align='center' width='100%' style='max-width:560px;'>
                                  <tr> </tr>
                                </table>
                                <!--[if mso]>
                                        </td>
                                        </tr>
                                        </table>
                                        <![endif]--></td>
                            </tr>
                          </table>
                          <table cellspacing='0' cellpadding='0' border='0' align='center' width='100%' style='max-width: 680px;'>
                            <tr>
                              <td style='padding: 10px 10px;width: 100%;font-size: 12px; font-family: sans-serif; mso-height-rule: exactly; line-height:18px; text-align: center; color: #888888;'>
                                <img src='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAeAAAABiCAYAAACI2wDyAAAd9ElEQVR4nO2dB5gV5bnH37OFZQuLKAKKDUUw2KIGxRYjUWPvSMQWsRtjieVaosbEXPVqYrkJxoqIKFchIlEs0QhqbFFseCMoKE1BEGHpbLvPrP+5jl++NnPKzjn7/z3PPrt7Zs6c70z53u/tQgghhBBCCCGEEEIIIYQQQgghhBBCCCGEEEIIIUVFZsmeu5fyFessIheIyCki0gf/TxaR/USkOQXjKwm6VVbIhIYVMmzhYulVUS5lHf2EEEKIBxUlfJJOFpG7IHSj/EhEBojIB+07PEIIIR2ZUhTAlSLyhIgcZNi+VETmFHhMhBBCyHcoNQHcQ0ReFZGtLPssEpFlBRwTIYQQ8m+UkgDeSETexm8bM9t3mIQQQoiUTLxMlYj83UP4Cs3PhBBC0kCpCOBHRWQbz33n5nkshBBCiJNSEMCXicjhMfafkcexEEIIIV4UuwDeWURuivmeL/I0FkIIIcSbYhfAd8Tcf62IfJynsRBCCCHeFLMAPktE9oz5niAFaXGexkMIIYR4U6wCOIh6/lWC900XkaY8jIcQQgiJRbEK4ItEZJME76P/lxBCSCooRgHcBQ0WkvBh+w6dEEII+YZiFMCB77eXYdvbMDObmFe4YRJCCCFmik0AZyCATYwQkZWW7f9bmGESQgghdopNAB8vIn0N255Fi8GdDdsbRWRBHsdGCCGEeFNsAvgUy7YxItLJsj3I//08D2MihBBCYlNMAjjQbA+wbJ+IRvsmKHwJIYSkhmISwEMt215Hj98fWfb5IIvPDto2XioiL8KP/IrDF00IIYQ4BUuxcJRlnKPwe3vLPknbEK4nIi9pjh1U4aoXkZuL6BwSQghJCcUigH8iIltbto/F7z6Wfd5L8Lnri8g/RWRLw3abz5kQQggxUiwm6OMs26aIyFIR2UlE6iz7JckBft4ifIXN/QkhhCSlGARwjYgcZtk+Ab9tGvKcBF2QxkCo21ge85iEEEJIG8UggPcTkQ0t25/Gb5v/d2bMzzxHRIZ57PdlzOMSQgghbRSDD/hgy7ZXIqUnbf7fGTE+L9Ckb/fcd0WM4xJSCgRxEfujq1iLiFSiz3ZQCGcNrzDpYHwfP8vwtbsgU+Yt/H8gSie3IBV2KbYF7tJdi0EA72/Z9tfI39tZ9psW4/P+hEnFRVBZa0mM45YwGVnV2iprW1ulXERas/+iQXR5tYg05+icleMBKVUBkevzFSWYIxpEZBVe6xcJegxpxgJ4bh4+Pw1U4xzHOb8ZPArBuVtXoueFiJytSUn9HxH5Kf4O7oHbRORlEXlKRO4Ukc1FZIdAfqVdAO/lCIIKzc+Bn3hjy36feH7eaQ6BH+VrNvcHrS2yaUW51JVlZE1rq1RlMtke8V4RGQItK1f3aHBt78/RsdJGkIZ3ZJ4WGJ1F5GoRud6yT3OehH9aCCbYW6Hp+64vowJ4IeoQPCci42k5K3miPeefxYL1L4hDCuKGfgYl7/20C+D9LNumRopr9Lf4iYPV5/senxWYBH4bY2zLaXL7hoamZtm7tkaOrKuRBxtWSL/Kymxn49ACkcv7M+tVQYqpxtA652mIruuQA6NHqgnTDasSDDK4Jj0QozIMi8ugdsCvYJYkpYf6PHSDfAncNw+IyCFQ3samXQDbtNFnIn9vZtlvtmcZyitEZKMYY2MAFmhb7rWKnFtfL+NXrJLVra3SKTstOHzznxEQt3GWAjS4zxdlM6CUU8raZxrIpaCswFwTCON9MD+R0uYFBAKvgxVkCuazVWkWwIFQ3c2yPSqAbf5fnyb8W4jIJTHGJhTA3xLcScuammS3mmo5trZWRjUsl36dstaCJRLYwDrednxiFki6CPyAb8AXyLmktLk78u3+Ff2maRbAP0TwjI7ZiIAOsfmJ3/X4rKsSVLXiQxOhTdi2tsrZXbvIuJUr24KycuALNl1/8l3mQkv7zHBegoDBrojGjNKKHPl1lnO9JeIdyHdZg2jXVkM6ZzPmlL6IUdHRU0TuEZEjeG47JmkWwLbGCpMVO/s2ln2nW7YFfE9ETo85NmEE9HcJRO3SpiYZVFMtQ+pq5IFlK3KlBRM3pznu4Vak8z2lvL4ONc3nl7iPPB8EJsVdPI4b+Of3FpHficgPNNsPx1w3Of1fmeSaNAvgPSzbno/8XQUTsgmXBnxxwvF9kfB9Jcs3jrJWObO+i4xbvipXWjDxwxUIpVsLtUZez0cgVS8EIAU3wVcJy8HqCBqk9IbW3oTjNuR26E58b+zV8PsFPyMRAatyTEwBXAYXXT3+b4AVJJfr3SqYycPAs5WwljQ53udDJxy7GvfdMow/n8F81ajxsMzT714eOcfB1LYgH3EkaRXA20Iz1dGM7kQhm1hSkL501GseAO0hCRTACm2+4MYm2b2mRo7tUisPLMuZL7jQlEO72TaSRD8fDT3itrXMREyUtlPRH/f8zCxbZ5rQmZgzeaqGFwTOnauJzfgUOZI3oSBBHOpxzGMRURx1Ga2GOfgx5PEXIs0nycryNKRW9lVe39Xz/YNE5OciMlgz5y2AWy7IM/17grEJ5MHJInI87v9uyvZAAL2NMr2PxBT4NTj2EPR2DxZRUebBJ/5QpLywixNE5IJI/nlvjCtaSGkPuBh/iEjkcRiDiSDu6Hyc46jLpgXP5SjcYznJ7U5rKcrdLdteU4RqP8u+MyMFBHSca9m21PEgx51AOgShL/ic+i5SW17WpgUXEd1F5Pfweb6BvOH/FJEbRWQ00tmCie7KGCk/20NrmIkqOVGqkWMb+G4/EpHHReTaIr1PFmECD4JMRhgCI4NiHZdjYXxAjGOfh+ftBggGNV6jGq/fiGuXxKVUCFoMwqWvo5FMDwjVYO470aBw9MLi5AXcu5vG/D4nwK12H9I/VeErSPU8EM9CcA0P9Tz2+bgud0KwqcJXoEgdg2dgHj7HRXDNB4rI0fjZTbGc/lFE/gH3S3h+TRNSV7hoXkeEuhovEcjKHUXkD/juoYs0qwkurQJ4T8u2Kcr/tgAsWwWswLxwhmX7dQ5NZKFlW4cljIjetaazDKmtlXmNTcUSSfULCJFfQlAuwmRwJ6IYJ0OQ9oQ/b6ln8ExowttI0ZoOxYT3G5jjQooxN3Q+onmnOeIxQipRoGAfj30DjeW/Y2icFQhsustz/0KjKwpUYZmL94JA2jfGOHfFom4vz/0fheapLhBtrI9KhLdZ9gmO9yo00jhBrr1RZMl1DXV1GEKl6R5YC3wYAOuMrexxlEBYv4gSlFm5VdIqgG3+35eU/20pSLMs20633BRTsJLc2bB9DTQhouEbLVjkrK5FowUHvrk78PdruP96YFV9Lioh7YtV9FkQnFXQZlwaayhQMxGT3aWYvDrjWL/Hw789StsVG10hKPvHHPfdDsE6HlpREs5EwYu0Ua0ZT5PBnLsbShgmSTMrg9Y8wLHfcw6TrIsLsEhVWR8WI5s108WZqCAVh8+gwftaQXrjHOs0fhdPejbtMVIIH/BAXKTv4WELfi7UaLIhO1rMyg1okB9lc8O+YmnCX6ep3xnlj8g9NVW+Wd4OQR9FQzQi+ri6WhmZbl/wnZHAmIthYjKxFkIjqGY0ERVtfo1V9+8dn9OKQJZAa/4vvHYDzNnFzgb4CXkH/sgmPP8mTSx4zofD7KlyJRZAKl/i/D2HZ7AnhPSFmgX1JbBcqNHf7Ymuut8M3BtROmtqbodMwhw1Hb79QAm5CNHWUSpxbk1C8E+WYkczI+UTqzAvH6Vc55AXNK9NsATHTsV3mItj74Jj12v2PQrX+zLDsVR2cWiyqtI5GosFHfMQsxAG8u6EGs+hC6A3fhKTTwFcA4f44Zpt4/Hw6VJ5bNrv60pOYsYSrCUwjek4HhqOjtVYzdvSoBZgP2KgTe1rDSKi6+SxFTnLC841J0U0zjNhtvKhBSbkx+B3uwWmttcs7/0cwiicVA9NmWDIBYshNF9WjrUNCufoFsuHaATw1oaysC/DEhFdywURrW/C7/yqxj96bYrO8wH4vipvaV67ziDATkU5wygfw11yNVwaUQZhgam+5yeWGJhzUIVO5Qwc/+rI6wcpRZEC/kOzGBA8A8drrJjh97oN/mKVS+Gy0Al6lYM1AYezYHFajHsk5KcW0/5ViP+I8hAW6ddi4Z01+TJBb4oVlE74ClZRRxm26S5ciKo197SUj1xgyQG2mflG4fcOln1YmclB6AseFERE1yX2Beci5cFEDaJxBQLAV/hGGYbJTzQPq0oGJtFAsxlagsK3BT5dVfgKAswGGyJHdW6eCzRzU/As/9gSeTvbMN8MhJDINWtjHu9gpXtblMeU/zeEEFS5XCNIo/zWYE3Q+UKvMBxjsEH4hlwjIofh79M0wreH4difYCGmE74CC9EFEMQ6rrKMKUpl5N5ZirFuBQvJ9UqE+C8MxzjR8Txfh32yJh8acDXydNUoMpVdNDdLhUMAqxdvgMVM/IXhIdnf4tsNeBC/Tc0dhEU4/IhGRI+LpwWHTuOh0IZqE3x8BpGVwYP7hGb7cCzemrKIPG7E8R+FxeTHllX65vgZhf1LjduQCmRiFqLK1cVvDyykw6DGYGF0nOYY1+J823gbc4qaWnhkpHNartgAc0mzISK+CZpYf9zHgwyfO14zrw3RBERNiywYbfwGlp2oOf4HCBgKTal7GQLgrkBwkYsncc10mSAnIyZA5QS47lw8AOXnImW/fTFvT/U4hmBBtpOlitsAg7X1PqRZuRiDYGHdQsmbfAjgex2pQSHdNa/tjklTx1zFfCCOUHvTZKBLhA+ZHDEj9rTsl6uCAiXNtxHRQXWsWhkZ1IiO1ympD36yYUeDAA4n+dEWV4UPj0E76w9fpMtMdmOJXnOfYJm/aQRwpRJkNEiz+J0JX5wPozUC2CT8smFT+KGzYY4hE0Pnlx2leU3HHNzvamDV7hEBrDv+bEf8g4qpFK/O/zoGbgJfboBLSF14HxRDAA9xlFA15V7/McY4b8H9nNi3lmsBfH6MqDBdByNbqP0LmhSNHS3765qDb+UY38jI3yYfsUQqqWwIs8muWN1NwwTg23+45Am14LPqu8iEFd7VscIdnkF0cs8EN3kGQR26SXKjSKpbLkzBz0IAuyI+J8EcW2o0elYX0rlumpXnWleuMRQc3R33QVhta5VSf3lbBMtks9DKNdNRHEInJLbXvBamVNoscxkIxmkaAbxT5O+BmvdOykFxiTqD6063ALaxCFr2UGUnW3OeKH/WBOuq7KR5bapn74CQWbAYDI7xnu+QSwG8n1KBxIXuRrIl5+tMSCZtWQw5wDrTVshiZRVvE8BvwhzyljIhHA0fyXBFmOvYEEEQW2LCaEDkoC2Qpz3pEzHZrkXeXAOS6juZVsRhRPRuNZ3luC61cs+y5dKn0vu2m5oH06HgnId+on859vUhXN33RTqDaeWt+stKhSYP87AvOtfVgTinnRyFDzIYhxoNXY5I1zQI4Bb4Ik1uj/U156AFFgCfMIoWwyIlGq2rC4bTBYLFZQtDlLStHoOJNzUC2DbfR3nEYx9d5LNP5zyV99MggLdLoEl0g4YSpvP0sRTgWArzle5zTehW2ydZ9h+jVL7ShcQLhM8N0HZMq/H7kQJlMpccDdOlGmhyGfyISUvJ5ZN6RPBOxyrxJEwUIyDQjGa+b9SbjGxQXiYt8QrHxO1Q5UvouliZI3dCuPiocwjgfJSYTANlOexcpYvpSBIDoNIrBef/gki+uYn1NN+3zDIf+RK1COhyXnPR8UpXzWtFwhrKulK/PoVCFnpov2IYa5LiSss89jGSCwG8Fcp9xZ0su8G0GApgW/P9SZobpN4S6LVOEwF9mCNl6aHI31WW3LAqCEkXVxgS3C+N5IHqOC2lAvg95MB+hDzPM1ERqpfLL/PNKqVVGltSU5AjHEi+aiGb+LSAn1Ws5CtdPNcR9QsRKduizH0LobkdpnmPrgSjygqkOOoKduQKXQplnCpYJnRWkDrMpYtjHku36FJzpXV87JkiqtsnSTEOWwlRJ9kK4C0RGGVanTU6VsdRIWdKWRJDgMfWlhO2WHPBVXNGlDcUE0w3gyklDmHR+Oiq+zSH8BUEGtSntNBHLSaGjaDB/wTn2asqWFl60oBDrbcGZrM4fh8dYUDh15bglEbWD/dCd47eQMCLK7NCRzjHvZ/jcX5u0WbHweyqNl24Douw0ZbjNuBHFcA34j61+YBNrKfc47M1JXy3TXBclbka/7tA8ZkR81i68fjMM77PmG6/JOfAp/SqkQqcnEuQwB6u5MIm081Qsb/Cyf0IxTDmwfz7iiHkXJAScCIqBm1t2CcUwJtbim8vMvjObBWwZigBBd0ddXtVn0FtjsyfwyI5cfsgQtxFNwhhU9RnPwR99cc4V0Pbf9KSHrUBrtcG0PrCoJdyPDDL8XsRTD8mf94LEDCrkS6QwQrQqrW0fVgmIxXpaTn7Ge7xDAL5shXAYeDQx44GHkXVmaKd0F2LMgi1NGFrxrEWi21dtb+7MG+arCFrILzVLIwPYkSCu/hIE/B6IApoZMOXCEBVA7GOSBCIpYsHetvjfb6Ki65K4iDMq64e8iE9Ytbo/jcqkDgfR9tbh2CGDS3q93T4c9c6zAHhinaoRUueaDA92Gqcqrb8oZaxrtKUfEuyytQxPCKAfXLLQo6OPGxVuBmPwG9b6lWQa/ew8trthuoyJhYhuu8DWAVejKxeo32YP/Y9YNeyMlne2CRT1qyRrmWpaM0wH5Pg3jivvikeJsLFo6m8KvHnDcwx0QXwQFhbni2i8/gSNF412Koai1dbI4pXNW6uCzXPdlKe0+Sv7gCFIdvPeFYjgE9FERrfgMdTDPO7TxCjrwvD5Cc+F756H87TaPuxKEsQddsJAVMmgXYf1PKwCIatb27od7BVFTFFtNk0YPVCn2DZ9ymNwDb5f3XMsQQwhFVh7ohZM/QAnJObYOKdiBW1q8WYmtq1qaPloo4NEe5/OsL5p0PY3mpIX7ASqJgV5WVy69IGeWXVGulRnpr+H2H1oaMcwXwujo5MFqbavcSf+QZBqytNaWJvxzNfKH6tqV0gSD+yVXbSudwGOrq3qZxqUVKeNKRpjrDUb1Z5zlDxyvQMPGh4XWUzQzbN64Yqayq+ZrapBi34fM9WiD9USnImogLBAr1hzmzEF6jFT1eYRLtFCq6vj5+uEKBVELZv4sSpBTBs0XUNsLvr8t4EJgdTYQPfGtA7OPIzx2tes6UgqWwHgaWL0haPEoU61nP4iQSmzmkwZX0IE48a7j8XWvixkRSNKM1YhPXBdTBVFeuLFfiF+LywRZ93BODSluY2C2xZJiMt6eiO9AAmwZ7wy/u2IlMJBcP4GEUCiJ07NEFMAyGYdA0aouwL4VCB+coVdZxvhsP/rLq0rodA0ZVmfBcLRDWI8264mXRzlrrfGVAOdtUoGE1YUKuFN7pCMzzEUjhjC6RL7ojA2fmKcJ0Kpel45X0/wHk4xCD8BfP0Xw1uzVsc3zkJd2HRofI06kSbTP7HaMqHJiIMUMhnfpzt2Kc4ejY+ZHi9zJETFi0KYKo5LbiZJ2le940IvBH+0+fxo+t0YuJr+ATjaNuhuXwsHlyferSjPYS5IPBrIExjB6NUqI4+EFg3oX7yNbbw/TZR29Iil6zXVZ5ftUa+aG5uM0mngOWorTsSPvdgQvxVzGE9DC2jEcciueF5XBe1LvBRmLyvgeUqDHgL792LlGYHt0MA5aRub0KmQ6vS1Ve+L3L/qFwJ94gquMdh8r8ZGtw6zIeboeTmFREFYjOcr0M0CsKtEPCqctIdboCn4TYL/aH9IHjUBdAoWDyjXb0uhhapBsluj7n5ETSPmIUqaNvA/G2qAzHGY9GRhDth6dOVJh6L63Y/Fg6tGP/wGH2WnRSiHaEtz9LWcWiJxWyxqcMEHQ1wsPUTfcJQn9THBzwLE0HIJTGDeY5HIMcEj33nIvf4/gRF4H1pgLXhBXyvjfFAn2iwIGSQinQmVtvGALMlzS2ycVUnObC2Rm7+emlaBLBACx6E1pRXYfLx7cc7IRLYdxKrn+WcM1Ct6PvKgTfBc9CC56IJr5msN3vD79qe3cvugraozkV9sW245j2fQEHRueCG4KcBi996S+ncSouFbijmLJ0ScFCMJhaqqfsLLJYma/bNQNj6Vkx8M88LqNMtlqs9HN35PoUiZestYKUQM2HSBO+Rlqhemz+1AVHbAju9ybwtFjOCj1Z6mbJyfS+GBnU3/FxPOKqvfIni5pthtZYv4avjc5hn9sBi5zqLP19XPvD/CQ3OVUEeUvrigM+OBGGdhQjpYZaCEOfALx8K35NzGJ1KvqUZmoZpUVuG+3Iri/D9AM9/GlqHnmFIoznVIozGGoRzSD0yTGx166+AdUfHXFgObHE6Lu7FQl1liqZ1ZFwm51LbNPCOowKjjaGeNdCNFEIAz0nwnhaDbT6kv+PzQqGvK4QRMtsSVenSgMcZTCK/8yjQPhoTfcgwBMLNQbDTLASRXYkHy8d8nG/mIKBkY/SxHY9xzocp2qo1ttV+bm6RGWvXSVU87bfSY59c8LOIK2RzmLyW47o8CFfIO/C7j4BvcQ7M9K7rE37h9l566E58RY4qWGVzHNt7V0ILvi3BcUcg/iMX+fQ6S2HcufNr5bmPMsbi8hkJf+vMmJ+3CALQ1fxjFtxKcQMIW7GosAWGTUbNgOct+5i4BOO3lTjVpYommTP+Bpnia8VqwQL8nwZXqPcYCmGCTtI79x7cGCZsQVLRAhy6ajQhkzTNHUJsZd/WQfs1cST8szqt8FJNMMH7DjNH2ngqbtnRuvJyGbd8hTy+cpVsUuF1y4U+fFOwRj4YgTaBF0Pr6AHztFpi8wP4z1y1vkNWYvKdbbnfCsEyWFSWwGxbD0vRmpifvQoLr5V4FuIcJ+l7L0KXml/CtGnqAf4VNJJbEhR+sLEAi6/ZEDy9YuSKRpmIoMzzIopJIxYZP7dou+/DXH0setjuZVkAvA2t9O4Y99tauMRuxBx1mGUOnINF6a2ebVkXwfy+G4TqgZYMmplY0N7uWVBjLqwbs3Bd+mRRbW4GrAkn4j7TNWtYge9+TcTK+gXu5fB+izWGzJI9XQ1csqYnblyTmUilGTfbZ5Z97rRoXQ/Cd7Kvo6TjoRZB8qYl5eZyj76c5QhW2C5SC/W5jtrIv1tlpVy96Cu5fslS2bpTZbtKoRhsGyle0ogH7Z2UddTpqGyHwJ2wfsFXsB5Ny2Mpy3xTj4WIT0eibtCKN0PA6ErMLe9aKrHFoRLH74eI5Cb4mv8VJ//fQGdYJoJKXN3wbC2EK86mdBWa3jgH9bin5sFXnFM3YCE04IVYqahl2Uzc5BC+4ihJF2pPtujnhQ7hPNYggMd7NsVuhn85J6HqpUBdYHrOtK8KGJMPE3ZHIflnWsIOO2kmjqn8a0OAU65oRBGeXHRIUlkDBSdOf+D2YH4hFtuFEMACs7CPAP7MM+HeFiQVCm9bXuczjsCMPyCwZncE3qyE9hPXD0NAespAE0JIOiiUAJ5ra1cX4TxPX1J3y7b3oCFvZdlnosdnzE9h/VlCCCElQqESMn38Brd4BvdUO6KU5xrC4kO+8ohUJoQQQvJKoQSwq0DFXxB950ONowB2d0drwwmOjjWEEEJI3imUAH7M0o/zbke1KpX1Dc2aBf7fIKVnsGMshBBCSLtSKB+woL3WL5E6sAg5apPQ5SIO61n27Y6KTaaUp0+KrKUZIYSQEqWQAnixUrA7KbpOGSF1liRvSdAUmhBCCMkLqamKHwObgHXxeJq+CCGEkI5LMQpgW5lIG0EVk38UZoiEEEKInWIUwDYTtA1Tb2FCCCGk4HQUAdycp4bOhBBCSCI6ig/4qYRtEQkhhJC80FF8wA/kYRyEEEJIYopRAFfH3H86o58JIYSkjY5ggn40T+MghBBCElOMAthWB1olCL4aWZhhEUIIIf6Uugk66On7aR7HQgghhCSiGAWwqRGDjnsLNyxCCCHEn2IUwJWe+00RkRfzPBZCCCEkEcUogLt47vfnPI+DEEIISUwxCmCfMb8jImMLMBZCCCEkEcUmgDOeJuibCzAWQgghJDHFJoDrPIKw3hKRRwo0HkIIISQRxagBu7i2fYdICCGEuCk2AbxKRFZbtj8sIpMKOB5CCCEkEcUmgJtE5CPDtqDb0bkFHg8hhBCSiGKMgr5cRBbg7wYRWSwir4vIYBFZ1s5jI4QQQgghhBBCCCGEEEIIIYQQQgghhBBCCCGEEEIIIaTUEJH/A1UwZuznBg0cAAAAAElFTkSuQmCC' 
                                width='240' height='49' alt='ForTheRecord'/>
				                  <br>
                                <span class='mobile-link--footer'>Breezy Limited, 1401 17th Street, Suite 525 Denver, CO 80202</span> <br>
                                <br>
                            </tr>
                          </table>
                          <table cellspacing='0' cellpadding='0' border='0' align='center' width='100%' style='max-width: 680px;'>
                            <tr>
                              <td style='padding: 10px 10px;width: 100%;font-size: 12px; font-family: sans-serif; mso-height-rule: exactly; line-height:18px; text-align: center; color: #888888;'>
                                <span class='mobile-link--footer'>DISCLAIMER 
				                <br>
					                The following conditions apply to this communication and any attachments: For The Record reserves all of its copyright; the information is intended for the addressees only and may be confidential and/or privileged - it must not be passed on by any other recipients; any expressed opinions are those of the sender and not necessarily For The Record; For The Record accepts no liability for any consequences arising from the recipient's use of this means of communication and/or the information contained in and/or attached to this communication. If this communication has been received in error, please contact the person who sent this communication and delete all copies.</span> <br>
                                <br>
                            </tr>
                          </table>
                          <!--[if (gte mso 9)|(IE)]>
                            </td>
                            </tr>
                            </table>
                            <![endif]--> 
                        </div>
                      </center></td>
                  </tr>
                </table>
                </body>
                </html>
            ".Replace("[INSERT REDEEM URL HERE]", inviteRedeemUrl);
        }

        [OpenApiProperty(Description = "Flags whether this user is currently active")]
        [DataType(DataType.Text)]
        [JsonProperty("activeFlag")]
        public bool ActiveFlag { get; set; }

        [OpenApiProperty(Description = "The name of the department to which this user belongs")]
        [DataType(DataType.Text)]
        [JsonProperty("departmentName")]
        public string DepartmentName { get; set; }

        [OpenApiProperty(Description = "The unique id of this user")]
        [DataType(DataType.Text)]
        [JsonProperty("id")]
        [JsonRequired]
        public string Id { get; set; }

        [OpenApiProperty(Description = "The Active Directory id assigned to this user", Nullable = true)]
        [DataType(DataType.Text)]
        [JsonProperty("msAadId", Required = Required.AllowNull)]
        public string MsAadId { get; set; }

        [OpenApiProperty(Description = "The name of the user's role for display purposes")]
        [DataType(DataType.Text)]
        [JsonProperty("roleName")]
        public string RoleName { get; set; }

        [OpenApiProperty(Description = "The name of the title of user", Nullable = true)]
        [DataType(DataType.Text)]
        [JsonProperty("titleName")]
        public string TitleName { get; set; }

        [OpenApiProperty(Description = "The Active Directory id assigned to this user", Nullable = true)]
        [DataType(DataType.Text)]
        [JsonProperty("usageLocation", Required = Required.AllowNull)]
        public string UsageLocation { get; set; }
    }

    public class UserCreateUpdateParams
    {
        [OpenApiProperty(Description = "The access level of this user (determines access to teams channels and recording policies etc)")]
        [DataType(DataType.Text)]
        [JsonProperty("accessLevel")]
        [Required]
        public string AccessLevel { get; set; }

        [OpenApiProperty(Description = "The id of the department to which this user belongs")]
        [DataType(DataType.Text)]
        [JsonProperty("departmentId")]
        [Required]
        public string DepartmentId { get; set; }

        [OpenApiProperty(Description = "The user's name as it should be displayed")]
        [MaxLength(256)]
        [DataType(DataType.Text)]
        [JsonProperty("displayName")]
        [Required]
        public string DisplayName { get; set; }

        [OpenApiProperty(Description = "The user's email address. This must be unique in the system")]
        [MaxLength(254)]
        [DataType(DataType.EmailAddress)]
        [JsonProperty("email")]
        [JsonRequired]
        public string Email { get; set; }

        [OpenApiProperty(Description = "The given name of the user")]
        [MaxLength(64)]
        [DataType(DataType.Text)]
        [JsonProperty("firstName")]
        [Required]
        public string FirstName { get; set; }

        [OpenApiProperty(Description = "The url to enable the user to register for the system (system-generated and read-only)")]
        [DataType(DataType.Url)]
        [JsonProperty("inviteRedeemUrl")]
        public string InviteRedeemUrl { get; set; }

        [OpenApiProperty(Description = "The family name of the user")]
        [MaxLength(64)]
        [DataType(DataType.Text)]
        [JsonProperty("lastName")]
        [Required]
        public string LastName { get; set; }

        [OpenApiProperty(Description = "The user's contact telephone number")]
        [DataType(DataType.PhoneNumber)]
        [JsonProperty("phone")]
        public string Phone { get; set; }

        [OpenApiProperty(Description = "The id of the user's role for display purposes")]
        [DataType(DataType.Text)]
        [JsonProperty("roleId")]
        [Required]
        public string RoleId { get; set; }

        [OpenApiProperty(Description = "The id of the title of user", Nullable = true)]
        [DataType(DataType.Text)]
        [JsonProperty("titleId")]      
        public string TitleId { get; set; }
    }
}
