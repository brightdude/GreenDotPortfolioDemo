# Test Configuration

Install dependencies

```shell
npm install
```

Add a settings file inside the integration-tests folder `.vscode/settings.json` with following contents:

```json
{
    "mochaExplorer.files": "dist/src/**/*.spec.js",
    "mochaExplorer.require": "source-map-support/register"
}
```

Create an .env file based on [.env.template](.env.template) file.
