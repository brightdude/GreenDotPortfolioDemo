import { ClientCredentials, ModuleOptions, AccessToken } from "simple-oauth2"
import Axios, { AxiosRequestConfig, AxiosResponse } from "axios";
import { Observable } from 'rxjs/internal/Observable';
import { shareReplay, switchMap, map, take, catchError } from 'rxjs/operators';
import { Settings } from './settings-helper';
import { from, of } from "rxjs";

export class ApimOauthHelper {
    private readonly accessToken$: Observable<string>;

    constructor(private settings = new Settings()) {
        this.accessToken$ = this.getAccessToken();
    }

    async get (url: string, config?: AxiosRequestConfig): Promise<AxiosResponse<any>> {
        const token = await this.accessToken$.toPromise();
        if (!config) config = {};
        if (!config.headers) config.headers = {};
        if (!config.headers.Authorization) config.headers.Authorization = `Bearer ${token}`;
        return Axios.get(url, config);
    }

    async post (url: string, data?: any, config?: AxiosRequestConfig): Promise<AxiosResponse<any>> {
        const token = await this.accessToken$.toPromise();
        if (!config) config = {};
        if (!config.headers) config.headers = {};
        if (!config.headers.Authorization) config.headers.Authorization = `Bearer ${token}`;
        return Axios.post(url, data, config);
    }

    async patch (url: string, data?: any, config?: AxiosRequestConfig): Promise<AxiosResponse<any>> {
        const token = await this.accessToken$.toPromise();
        if (!config) config = {};
        if (!config.headers) config.headers = {};
        if (!config.headers.Authorization) config.headers.Authorization = `Bearer ${token}`;
        return Axios.patch(url, data, config);
    }

    async delete (url: string, config?: AxiosRequestConfig): Promise<AxiosResponse<any>> {
        const token = await this.accessToken$.toPromise();
        if (!config) config = {};
        if (!config.headers) config.headers = {};
        if (!config.headers.Authorization) config.headers.Authorization = `Bearer ${token}`;
        return Axios.delete(url, config);
    }

    getWithCatch(url: string): Promise<any> {
        return this.accessToken$
            .pipe(
                take(1),
                switchMap(accessToken => {
                    return from(Axios.get(url, { headers: { Authorization: `Bearer ${accessToken}` } }));
                }),
                map(response => {
                    return response.data;
                }),
                catchError(e => {
                    console.log(`ApimOauthHelper.get error: ${e}`);
                    console.log(e.response.data.error.message);
                    return of(undefined);
                })
            )
            .toPromise();
    }

    postWithCatch(url: string, data?: any): Promise<any> {
        return this.accessToken$
        .pipe(
            take(1),
            switchMap(accessToken => {
                return from(Axios.post(url, data, { headers: { Authorization: `Bearer ${accessToken}` } }));
            }),
            map(response => {
                return response.data;
            }),
            catchError(e => {
                console.log(`ApimOauthHelper.post error: ${e}`);
                console.log(e.response.data.error.message);
                return of(undefined);
            })
        )
        .toPromise();
    }

    deleteWithCatch(url: string): Promise<any> {
        return this.accessToken$
        .pipe(
            take(1),
            switchMap(accessToken => from(Axios.delete(url, { headers: { Authorization: `Bearer ${accessToken}` } }))),
            map(response => {
                return response.data;
            }),
            catchError(e => {
                console.log(`ApimOauthHelper.delete error: ${e}`);
                console.log(e.response.data.error.message);
                return of(undefined);
            })
        )
        .toPromise();
    }

    private getAccessToken(): Observable<string> {
        const config: ModuleOptions = {
            client: {
                id: this.settings.AutomationUserAppId,
                secret: this.settings.AutomationUserSecret
            },
            auth: {
                tokenHost: `https://login.microsoftonline.com`,
                tokenPath: `/${this.settings.AutomationUserTenantId}/oauth2/token`
            }
        };
        const client = new ClientCredentials(config);
        return from(client.getToken({ resource: this.settings.ApimProxyAppIdUri }))
            .pipe(
                map(x => x.token.access_token),
                shareReplay(1));
    }
}