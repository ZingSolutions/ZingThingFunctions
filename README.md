# ZingThingFunctions

Functions API for the ZingThing

## Deployment Issues

This project uses an [Inject] attribute to inject dependancies into the functions that process this API.

Unfortunatly at the moment, the deployment of this functionality using Web Deploy is broken and deploys a mailformed **bin\extensions.json**

To get arround this you need to do the following:

1. Deploy the project to a local folder using Visual Studio.
2. In the deployed folder browse to the **bin\extensions.json** file.
3. edit this file to match the one from your builds bin directory in your project, there should be one extra line:

```json
{
  "name": "Startup",
  "typeName": "ZingThingFunctions.Startup, ZingThingFunctions, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
}
```

4. zip the contents of the deployed folder
5. stop the functions web app you are deploying to
6. use the KUDU API to deploy the zip file, e.g.

```bash
az webapp deployment source config-zip --resource-group ZingThing --name zingthing-dev --src .\ZingThingZipDeploy.zip
```

7. start the functions web app

The app should now be deployed successfully and any functions that use the [Inject] binding should now load successfully.

Note: you can run these functions fine locally using the debugger, it just breaks when you try to deploy.
