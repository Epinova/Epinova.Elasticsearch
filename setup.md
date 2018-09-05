# Installation

## Java
Java must be installed and the environment variable [JAVA_HOME](https://confluence.atlassian.com/display/DOC/Setting+the+JAVA_HOME+Variable+in+Windows) must be set and point to the bin folder of the Java-installation. 
Elasticsearch recommends Oracle JDK 1.8 or higher, but your milage and environment may vary. 
Read more [here](https://www.elastic.co/guide/en/elasticsearch/reference/current/_installation.html)

## Service
Download and install Elasticsearch version >5.1.1 && <6.0. How you prefer to setup thing is out of scope for this document, 
but you need to at least address running as a service, roles and [heap size](https://www.elastic.co/guide/en/elasticsearch/reference/current/heap-size.html).

## Plugins
The only required plugin is the [Mapper Attachments Plugin](https://www.elastic.co/guide/en/elasticsearch/plugins/5.0/mapper-attachments.html). 
This enables indexing of files by using the Apache text extraction library Tika 

Please install it per the instructions mentioned in the link above.

## Configure your project
Install the following Nuget packages in your project from Nuget.org

* Epinova.ElasticSearch.Core
* Epinova.ElasticSearch.Core.EPiServer
* Epinova.ElasticSearch.Core.EPiServer.Commerce (optional, for Commerce support)

Check that the config-transformation succeeded and added the below configurations. If not add them manually.

  ```xml
  <configuration>
      <configSections>
          <section 
            name="epinova.elasticSearch"
            type="Epinova.ElasticSearch.Core.Settings.Configuration.ElasticSearchSection, Epinova.ElasticSearch.Core" />
      </configSections>
  </configuration>  
  ```

  ```xml
  <configuration>
    <episerver.shell>
      <protectedModules rootPath="~/EPiServer/">
        <add name="ElasticSearch">
          <assemblies>
            <add assembly="Epinova.ElasticSearch.Core.EPiServer" />
          </assemblies>
        </add>
      </protectedModules>
    </episerver.shell>
  </configuration>
  ```

  ```xml
  <configuration>
    <episerver.framework>
      <virtualRoles>
        <providers>
          <add 
            name="ElasticsearchAdmins" 
            roles="WebAdmins,Administrators" 
            mode="Any" 
            type="EPiServer.Security.MappedRole, EPiServer" />
        </providers>
      </virtualRoles>
    </episerver.framework>
  </configuration>
  ```

  ```xml
  <configuration>
      <epinova.elasticSearch 
        host="http://localhost:9200" 
        bulksize="1000"
        providerMaxResults="100"
        closeIndexDelay="500"
        ignoreXhtmlStringContentFragments="false"
        clientTimeoutSeconds="100"
        username=""
        password=""
        trackingConnectionStringName="EPiServerDB">
        <indices>
          <add name="my-index-name" />
        </indices>
        <files maxsize="50MB" enabled="true">
          <add extension="doc" />
          <add extension="txt" />
        </files>
      </epinova.elasticSearch>
  </configuration>
  ```

The last 8 attributes in `<epinova.elasticSearch>` is optional and shows default values. 

Explanation:

* `bulksize` defines how many documents should be sent simultaneously when performing bulk updates.
* `providerMaxResults` how many hits to return in the UI search.
* `closeIndexDelay` the delay in milliseconds between open/close operations. This might be necessary to increas on slower servers.
* `ignoreXhtmlStringContentFragments` should content fragments in XhtmlStrings be ignored?
* `clientTimeoutSeconds` the timeout in seconds used by the underlying HttpClient.
* `username` username for basic authentication.
* `password` password for basic authentication.
* `trackingConnectionStringName` the name of the SQL connectionstring used for tracking.


Register your indices in the `<indices>` node, usually just one. If you have more than one, the default one used for IContent must be marked with `default="true"`. 

The `<files>` node defines which file-types should be indexed.
  * `enabled` turns indexing on/off.
  * `maxsize` sets an upper limit on file-sizes. Can be defined as a number (bytes) or with a corresponding suffix `MB`, `MB` or `GB`
