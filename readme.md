# Demonstration of Azure Durable Functions


**Azure Durabel Function explained be example**
This project contains sample code for demonstrating, how Azure durabel functions are working.
The scenario is a registration of a membership, that should be varified by eMail or SMS.

*Workflow:*
The user send e request for registration with his email and mobile phone number to Azure. Then he will receive a SMS and/or an eMail with a PIN. When using eMail, the user can click on a URL, that sends back the verification code; alternatively the user can pass the PIN (from SMS or eMail) into (has not been developed so far) a UI, that posts this to Azure back. 
If this happens in between 90 seconds, and the passed code is valid (no typos etc.), the users registration is verified. 
If the user passed a wrong PIN, a counter will track this three times and cancles the workflow at the end.
If a timout occured, the whole registration process becomes obsolete.

**Special to note: this projects sends eMail via a webhook build with Microsoft Flow, that passes the Http Request as an eMail**

## Getting Started

Download this code and check, that everything is installed, as needed.

Also see the presentation, I created for a talk in Hamburg, if you try to get a quick overview and an understanding of that topic.
[Slidedeck](https://github.com/totosan/DurableFunctionDemo/blob/master/Presentation%20slides/DurableFunctions.pdf)

### Prerequisites


1) Azure Subscription
1) [Visual Studio 2017](https://visualstudio.microsoft.com/de/downloads/) - Community Edition should be enough  
1) Azure Function Core Tools  
1) Azure Storage Emulator (for local debugging)
1) Azure Storage Explorer (far any cases of debugging)
1) (nice to have Postman)[https://www.getpostman.com/apps]  
otherwise you can use Powershell or VSCode to send HTTP Requests
1) eMail account

When using VS2017, please install also all Azure tools with VS Installer. After successful installation, go to **"Tools > Extensions and Updates"**. There search for "**Azure functions and Web Jobs Tools**" and install this also.

Everything should now be ready, to write Azure Functions. But one thing is missing. Writing Azrue Durable Functions needs to add a NuGet-Package into the project dependencies.  
[Microsoft.Azure.WebJobs.Extensions.DurableTask](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.DurableTask/) (time of writing the version was 1.6.2)

*If you like to use VSCode instead of VS2017, read [here](https://github.com/Microsoft/vscode-azurefunctions) for basics on Azure Functions.
And read [this](https://docs.microsoft.com/en-gb/azure/azure-functions/durable-functions-install) for the whole setup.*

### Installing

To get this working, you need an Azure Subscription, where you create all resources (see list below), to start with Azure Durable Functions.

+ There a two different ways to gt started quickly. 
1) create everything your self in [Azure Portal](http://portal.azure.com) and download the publishing file
1) use the deployment scripts in the *Deployment* folder 

## Deployment

For deployment, run powershell. Go to the deployment folder in this project and find **deploy.ps1** . Take care of the **parameters_fucntions.json**. There you have to change the subscription Id. Further you can and should replace the names and fit the location to you needs.

Then, you can run the **deploy.ps1** script.

for example like this:
```
powershell.exe .\deploy.ps1 -subscriptionId 69e5dabf-6be7-42fd-bb83-b066e03b4052 -resourceGroupName DemoTT -templateFilePath '.\template_functions.json' -parametersFilePath '.\parameters_functions.json'
```
This creates a resource group and the Function App.
What you also need is a way, to send eMails. Either, you are going the same way and create a webhool in Flow, or you can setup SendGrid in Azure. This project contains the setup for a webhook.
(The idea was, to see the flexibility of FLow and also having a webhook leads to more flexibility in sending information to whatever, if not eMail).

## Contributing

Please read [CONTRIBUTING.md](https://gist.github.com/totosan/DurableFunctionDemo/contributing.md) for details on our code of conduct, and the process for submitting pull requests to us.

## Authors

* **Thomas Tomow** - *Initial work* - [totosan](https://github.com/totosan)

See also the list of [contributors](https://github.com/totosan/DurableFunctionDemo/contributors) who participated in this project.

## License

This project is licensed under the MIT License 

