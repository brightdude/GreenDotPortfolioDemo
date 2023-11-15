import { CosmosDbHelper } from './cosmos-helper';
import { GraphHelper } from './graph-helper';

after(async () => {
    try {
        const graphHelper = new GraphHelper();
        await graphHelper.deleteTestData();
        const dbHelper = new CosmosDbHelper();
        await dbHelper.deleteTestData();
    }
    catch (ex) {
        console.error(ex);
    }
});