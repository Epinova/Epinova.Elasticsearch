# Installation

## Java
Java must be installed and the environment variable [JAVA_HOME](https://confluence.atlassian.com/display/DOC/Setting+the+JAVA_HOME+Variable+in+Windows) must point to the bin folder of the Java installation. 
Elasticsearch recommends Oracle JDK 1.8 or higher, but your environment may require something else. 
Read more [here](https://www.elastic.co/guide/en/elasticsearch/reference/current/_installation.html)

## Service
Download and install Elasticsearch version >5.6. Setup preferences are outside the scope of this document, 
but you need to at least address running as a service, roles and [heap size](https://www.elastic.co/guide/en/elasticsearch/reference/current/heap-size.html).

## Plugins
The only required plugin is the [Ingest Attachment Processor Plugin](https://www.elastic.co/guide/en/elasticsearch/plugins/master/ingest-attachment.html). 
This enables indexing of files by using the Apache text extraction library Tika 

Please install it per the instructions mentioned in the link above.

## Configure your project
Install the following Nuget packages in your project from Nuget.org:

* Epinova.ElasticSearch.Core
* Epinova.ElasticSearch.Core.EPiServer
* Epinova.ElasticSearch.Core.EPiServer.Commerce (optional, for Commerce support)

Add the following configurations:

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
          <add name="ElasticsearchAdmins" roles="CmsAdmins" mode="Any" type="EPiServer.Security.MappedRole, EPiServer" />
          <add name="ElasticsearchEditors" roles="CmsEditors,CmsAdmins" mode="Any" type="EPiServer.Security.MappedRole, EPiServer" />
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
        shards="5"
        replicas="1"
        trackingConnectionStringName="EPiServerDB">
        <indices>
          <add name="my-index-name" displayName="My Index" synonymsFile="synonyms/mysynonyms.txt" />
        </indices>
        <files maxsize="50MB" enabled="true">
          <add extension="doc" />
          <add extension="txt" />
        </files>
      </epinova.elasticSearch>
  </configuration>
  ```

The ´host´ attribute is the only required setting. The remaing attributes are optional and shows their default values above. 

Attribute details:

* `host` hostname and port of the Elasticsearch server.
* `bulksize` defines how many documents should be sent simultaneously when performing bulk updates.
* `providerMaxResults` how many hits to return in the UI search.
* `closeIndexDelay` the delay in milliseconds between open/close operations. An increase might be necessary on slower servers.
* `ignoreXhtmlStringContentFragments` should content fragments in XhtmlStrings be ignored?
* `clientTimeoutSeconds` the timeout in seconds used by the underlying HttpClient.
* `username` username for basic authentication.
* `password` password for basic authentication.
* `shards` the number of shards for new indices.
* `replicas` the number of replicas for new indices.
* `trackingConnectionStringName` the name of the SQL connectionstring used for tracking.


Register your indices in the `<indices>` node, usually just one. If you have more than one, the default one used for IContent must be marked with `default="true"`. 

Example:
```xml
<add name="my-index-name" displayName="My Index" synonymsFile="mysynonyms.txt" />
```

* `name` is the actual name of the index. It will automatically be post fixed with each available language.
* `displayName` is the fiendly name of the index, used in scenarios exposed to the editor.
* `synonymsFile` (optional) can be used if you have a predefined list of synonyms to be used instead of a manual list. 
This refers to a path relative to the config-folder on the server and must exist upon creation of the index. 
The files must be present on all nodes in the cluster, one for each available language, prefixed  with `<language-code>_`. 
Eg. with Norwegian and Swedish active, setting `synonymsFile` to `mysynonyms.txt` expects `no_mysynonyms.txt` and `se_mysynonyms.txt` 
to exist in the config-folder.

The `<files>` node defines which file-types should be indexed.
  * `enabled` turns indexing on/off.
  * `maxsize` sets an upper limit on file sizes. Can be defined as a number (bytes) or with a corresponding suffix `MB`, `MB` or `GB`
