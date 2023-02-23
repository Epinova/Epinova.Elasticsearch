# epinova-nuget-steps.yml

This pipeline is made for building and publishing nugets on Epinova nuget feed.

This version supports global.json, to use when targeting multiple versions of dotnet.

Version is updated during build [doc](epinova-nuget-tools/set-version.1.md). If a branch is run manualy a preview version of the nuget will be published.

Projects where the dll name contains "Test" will be automaticly run on build. If the dll name contains IntegrationTest it will not be run.

All of this is preconfigured in when starting a new project using `dotnet new epinova.nuget -n Epinova.PackageName`

## Requirements

Projects must be of the newer sdk type.

## Setup

1. Create your nuget project

2. Add properties to nuget csproj

    ```
    <PropertyGroup>
      ...
      <PackageDescription>Add package description here $(Gitlog)</PackageDescription>
      <Authors>$(GitContributors) @ Epinova</Authors>
    </PropertyGroup>
    ```

3. Add azure-pipelines.yml

        variables:
          system.debug: false
          major: 12 # Align with Cms/Commerce version
          minor: 0 # Up when breaking changes

        name: $(major).$(minor).$(rev:r) #Build.BuildNumber

        trigger:
          batch: true
          branches:
              include:
              - main
              - master
              - hotfix/*
              - release/*

        pool:
          vmImage: ubuntu-latest # or windows-latest for .net framework 4.8 support

        steps:
        - template: .azuredevops/epinova-nuget-steps.yml
          parameters:
            dotnetVersions:
            - 6.0.x
            - 7.0.x

## Overrideable template parameters:

      parameters:
        workingDirectory: $(System.DefaultWorkingDirectory) #default
        nugetVersionSpec: 6.x #override what nuget version is used
        pushPrBuildToNuget: false #default publish preview release to nuget feed on pr-build.
        disablePublish: false #default disables publish to nuget feed
        packageProjectUrl: $(Build.Repository.Uri) # if you want to link to documentation
        publishRepositoryUrl: true # hide where source is located; set to false for public nugets
    
## Tests

By default all tests will be run automaticly after build. 
Unless: 
- Project name is suffixed `IntegrationTest`
- Project file has `<IsTestProject>` set to false