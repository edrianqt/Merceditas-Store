\# Mercedita's Store



Mercedita's Store is a \*\*Point of Sale (POS) and store management system\*\* built using \*\*ASP.NET Core MVC and C#\*\*.



The application is designed to help manage day-to-day store operations through a simple web-based interface.



\## Technologies Used



\- ASP.NET Core MVC

\- C#

\- .NET 8

\- Razor Views

\- HTML

\- CSS

\- JavaScript

\- ASP.NET Core Session



\## Requirements



Before running the project, make sure the following are installed:



\- \[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

\- Git

\- Visual Studio 2022, Visual Studio Code, or another compatible IDE



You can verify your .NET installation by opening Command Prompt and running:



```bash

dotnet --version

```



The installed version should support \*\*.NET 8.0\*\*.



\---



\# Installation and Local Deployment



\## 1. Clone the Repository



Open Command Prompt, PowerShell, Git Bash, or a terminal and navigate to the folder where you want to save the project.



Run:



```bash

git clone https://github.com/edrianqt/Merceditas-Store.git

```



Then enter the project directory:



```bash

cd Merceditas-Store

```



\---



\## 2. Restore Dependencies



Restore the required .NET packages:



```bash

dotnet restore

```



\---



\## 3. Build the Project



Compile the application:



```bash

dotnet build

```



A successful build should end with:



```text

Build succeeded.

```



\---



\## 4. Run the Application



Start the application with:



```bash

dotnet run

```



The terminal will display the local address where the application is running, for example:



```text

Now listening on: https://localhost:7000

Now listening on: http://localhost:5000

```



The actual port may be different on your computer.



Open the address displayed in the terminal using your web browser.



\---



\# Running with Visual Studio 2022



You can also run the project using Visual Studio.



1\. Clone or download the repository.

2\. Open Visual Studio 2022.

3\. Select \*\*Open a project or solution\*\*.

4\. Open:



```text

Mercedita's Store.sln

```



5\. Allow Visual Studio to restore the required dependencies.

6\. Build the solution.

7\. Press \*\*F5\*\* to run with debugging or \*\*Ctrl + F5\*\* to run without debugging.



The application should automatically open in your default browser.



\---



\# Project Structure



```text

Merceditas-Store/

│

├── Controllers/

├── Models/

├── Services/

├── Views/

├── wwwroot/

├── App\_Data/

├── Properties/

│

├── Program.cs

├── appsettings.json

├── appsettings.Development.json

├── MerceditasStore.csproj

└── Mercedita's Store.sln

```



\### Main Directories



\*\*Controllers/\*\*  

Contains the MVC controllers responsible for handling requests and coordinating application logic.



\*\*Models/\*\*  

Contains the application's data models.



\*\*Services/\*\*  

Contains application services and store-related business logic.



\*\*Views/\*\*  

Contains the Razor views used to display the user interface.



\*\*wwwroot/\*\*  

Contains static files such as CSS, JavaScript, images, and other frontend assets.



\*\*App\_Data/\*\*  

Used by the application for locally stored application/store data.



\---



\# Application Configuration



The default configuration files are:



```text

appsettings.json

appsettings.Development.json

```



The current project does not require a database connection string in these files for its standard local setup.



If database services, external APIs, or other services are added in future versions, configure the required settings before starting the application.



Never commit passwords, API keys, access tokens, or other sensitive credentials to GitHub.



\---



\# Updating the Project



If you already cloned the repository and want to download the latest changes, navigate to the project folder and run:



```bash

git pull origin main

```



Then restore and build again if necessary:



```bash

dotnet restore

dotnet build

dotnet run

```



\---



\# Troubleshooting



\### `dotnet` is not recognized



Install the \*\*.NET 8 SDK\*\* and restart your terminal.



Verify the installation:



```bash

dotnet --version

```



\### Application does not build



Run:



```bash

dotnet clean

dotnet restore

dotnet build

```



Check the terminal output for any remaining compilation errors.



\### HTTPS certificate warning



For local development, you can trust the ASP.NET Core development certificate with:



```bash

dotnet dev-certs https --trust

```



Then restart the application:



```bash

dotnet run

```



\### Port is already in use



Another application may already be using the configured port.



Close the conflicting application or run the project using another available port.



\---



\# Quick Start



For users who already have Git and the .NET 8 SDK installed:



```bash

git clone https://github.com/edrianqt/Merceditas-Store.git

cd Merceditas-Store

dotnet restore

dotnet build

dotnet run

```



Open the localhost address displayed in the terminal.



\---



\# Author



\*\*Edrian Nathaniel Sangco\*\*



GitHub: \[@edrianqt](https://github.com/edrianqt)



\---



\## Repository



\[Merceditas-Store](https://github.com/edrianqt/Merceditas-Store)



\---



\## License



This project is intended for educational and development purposes. Please contact the repository owner before redistributing or using the project for commercial purposes.

