[![Build status](https://ci.appveyor.com/api/projects/status/vhaehgrviq4u92ha/branch/master?svg=true)](https://ci.appveyor.com/project/Epinova_AppVeyor_Team/epinova-elasticsearch/branch/master)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Epinova_Epinova.Elasticsearch&metric=alert_status)](https://sonarcloud.io/dashboard?id=Epinova_Epinova.Elasticsearch)
![Tests](https://img.shields.io/appveyor/tests/Epinova_AppVeyor_Team/epinova-elasticsearch/master.svg)


# Introduction

A search plugin for Episerver CMS and Commerce

## Features

* Typed search
* Wildcard search
* File search
* Range search (numerics and dates)
* Fuzzy search
* Facets
* Filtering
* Best Bets
* More Like This
* Tracking/stats
* Boosting
* Synonyms
* Pagination
* Related hits (did you mean?)
* Autosuggest
* Commerce support
* Highlighting (excerpts)
* Date decay
* Custom scoring
* Stemming
* Index custom types
* Basic authentication support
* Custom http client message handler

## Planned features

* Caching
* Compound word token filter
* Utilize aliases for better downtime management

## Version convention

* Major version reflects Episerver version
* Minor version reflects Elasticsearch version

# Requirements

* .NET 4.6.1+
* Episerver CMS 11+
* Episerver Commerce 11.5+
* Elasticsearch 5.6+
* Ingest Attachment Processor Plugin

# Usage

First of all you need to create your index. 
Go to the administration page via the embedded menu Search Engine -> Administration and status, then click the `Create indices` button.

This will create one index per language on your site. If you have the Commerce addon installed, additional indices for catalog content will also be created. 

![Tools](assets/index-admin.png?raw)

&nbsp;
A separate index will be created for each active language on your site. If you add more languages later, this process needs to be repeated.

&nbsp;

# Configuration

You can configure your setup programmatically with the singleton `Epinova.ElasticSearch.Core.Conventions.Indexing`. 
Only do this once per appdomain, typically in an initializable module or Application_Start().

A sample configuration class:

```csharp
public static class SearchConfig
{
    public static void Init()
    {
        Epinova.ElasticSearch.Core.Conventions.Indexing.Instance
            .ExcludeType<ErrorPage>()
            .ExcludeType<StartPage>()
            .ExcludeRoot(42)
            .IncludeFileType("pdf")
            .IncludeFileType("docx")
            .ForType<ArticlePage>().IncludeProperty(x => x.Changed)
            .ForType<ArticlePage>().IncludeField(x => x.GetFoo())
            .ForType<ArticlePage>().IncludeField("TenYearsAgo", x => DateTime.Now.AddYears(-10))
            .ForType<ArticlePage>().EnableSuggestions(x => x.Title)
            .ForType<TagPage>().EnableSuggestions()
            .ForType<ArticlePage>().EnableHighlighting(x => x.MyField)
            .ForType<ArticlePage>().StemField(x => x.MyField);
    }
}
```

Explanation of the different options:

`ExcludeType`: This type will not be indexed. Base classes and interfaces are also supported. The same can be achieved by decorating your type with `ExcludeFromSearchAttribute`

`ExcludeRoot`: This node and its children will not be indexed. Typically a node-id in the Episerver page-tree.

`IncludeFileType`: Defines the extension for file types that should be indexed. See also the `<files>` node in the [configuration](setup.md).

`ForType`: Starting point for indexing rules of a particular type. Does nothing in itself, but provides access to the following options:

`IncludeField`: Defines custom properties to be indexed. For example an extension method that fetches external data or performs complex aggregations.

`EnableSuggestions`: Offer autosuggest data from this type, either from all properties or selected ones via a lambda expression.

`IncludeProperty`: Same effect as decorating a property with [Searchable]. Can be used if you don't control the source of the model.

`EnableHighlighting`: Add additional fields to be highlighted.

`StemField`: Defines that a property should be analyzed in the current language. Properties named `MainIntro`, `MainBody` and `Description` will always be analyzed.

&nbsp;


## What will be indexed?

The module tries to follow the same conventions as Episerver, meaning that all properties of type `string` and `XhtmlString` will be indexed unless explicitly decorated with `[Searchable(false)]`. 
Additional properties can be indexed by decorating them with [Searchable] or with the conventions above.

&nbsp;


## Examples


### The simplest search
```csharp
SearchResult result = service
   .Search("bacon")
   .GetResults();
```


### Typed search
```csharp
SearchResult result = service
   .Search<ArticlePage>("bacon")
   .GetResults();
```


### Search in selected properties
```csharp
SearchResult result = service
   .Search<ArticlePage>("bacon")
   .InField(x => x.MainIntro)
   .InField(x => x.MainBody)
   .InField(x => x.MyCustomMethod())
   .GetResults();
```


### Autosuggest
Searches for indexed words starting with phrase:

```csharp
string[] suggestions = service.GetSuggestions("baco");
```


### Fuzzy search

With default length AUTO (recommended):

```csharp
SearchResult result = service
   .Search<ArticlePage>("bacon")
   .Fuzzy()
   .GetResults();
```

With specific length:

```csharp
SearchResult result = service
   .Search<ArticlePage>("bacon")
   .Fuzzy(4)
   .GetResults();
```


### Wildcard search

Use with care as this does not support features such as stemming.

```csharp
SearchResult result = service
   .WildcardSearch<ArticlePage>("*bacon*")
   .GetResults();
```

```csharp
SearchResult result = service
   .WildcardSearch<ArticlePage>("me?t")
   .GetResults();
```

### Geo-point datatype

Using properties of the type `Epinova.ElasticSearch.Core.Models.Properties.GeoPoint` allows you to perform geo-based filtering. 

Example model:

```csharp
public class OfficePage : StandardPage
{
    [Display]
    public virtual double Lat { get; set; }

    [Display]
    public virtual double Lon { get; set; }

    // Helper property, you could always roll an editor for this
    public GeoPoint Location => new GeoPoint(Lat, Lon);
}
```

#### Filtering options

##### Bounding box
Find points inside a square area based on its top-left and bottom-right corners.

```csharp
var topLeft = (59.9277542, 10.7190847);
var bottomRight = (59.8881646, 10.7983952);

SearchResult result = service
   .Get<OfficePage>()
   .FilterGeoBoundingBox(x => x.Location, , topLeft, bottomRight)
   .GetResults();
```

##### Distance
Find points inside a circle given a center point and the distance of the radius. 

```csharp
var center = (59.9277542, 10.7190847);
var radius = "10km";

SearchResult result = service
   .Get<OfficePage>()
   .FilterGeoDistance(x => x.Location, radius, center)
   .GetResults();
```

##### Polygons
Find points inside a polygon with an arbitrary amount of points. 
The polygon points can for example be the outlines of a city, country or other types of areas.

```csharp
var polygons = new[]
{
    (59.9702837, 10.6149134),
    (59.9459601, 11.0231964),
    (59.7789455, 10.604809)
};

SearchResult result = service
   .Get<OfficePage>()
   .FilterGeoPolygon(x => x.Location, polygons)
   .GetResults();
```


### More Like This

Find content similar to the document-id provided

```csharp
SearchResult result = service
   .MoreLikeThis("42")
   .GetResults();
```

Commerce: 

```csharp
SearchResult result = service
   .MoreLikeThis("123__CatalogContent")
   .GetResults();
```

Optional parameters:

`minimumTermFrequency` The minimum term frequency below which the terms will be ignored from the input document. Defaults to 1.  
`maxQueryTerms` The maximum number of query terms that will be selected. Increasing this value gives greater accuracy at the expense of query execution speed. Defaults to 25.  
`minimumDocFrequency` The minimum document frequency below which the terms will be ignored from the input document. Defaults to 3.  
`minimumWordLength` The minimum word length below which the terms will be ignored. Defaults to 3.  


Gadget:

![MLT Component](assets/mltcomp.png?raw)


&nbsp;


# Exclusion


## Excluding type per query

If you don't want to exclude a type globally, you can do it only in the context of a query:

```csharp
SearchResult result = service
   .Search<ArticlePage>("bacon")
   .Exclude<SaladPage>()
   .GetResults();
```


## Excluding entire sections/nodes per query

Exclude a node at query-time:

```csharp
SearchResult result = service
   .Search<ArticlePage>("bacon")
   .Exclude(42)
   .Exclude(contentInstance)
   .Exclude(contentReference)
   .GetResults();
```


# Language

The query uses `CurrentCulture` as default. This can be overridden:

```csharp
SearchResult result = service
   .Search<ArticlePage>("bacon")
   .Language(CultureInfo.GetCultureInfo("en"))
   .GetResults();
```


&nbsp;


# Pagination

Use the methods `From()` and `Size()` for pagination, or the aliases `Skip()` and `Take()`:


```csharp
SearchResult result = service
   .Search<CoursePage>("foo")
   .From(10)
   .Size(20)
   .GetResults();
```


&nbsp;



# Facets

## Setup query
```csharp
var query = service
   .Search<CoursePage>("foo")
   .FacetsFor(x => x.DepartmentID);
```

## Don't use post_filter when generating facets

Facets will be created from the currently applied filters and not the entire result.

```csharp
var query = service
   .Search<CoursePage>("foo")
   .FacetsFor(x => x.DepartmentID, usePostFilter: false);
```


## Apply chosen filters, ie. from a Querystring

One value:
```csharp
string selectedFilter = Request.Querstring["filter"];
query = query.Filter(p => p.DepartmentID, selectedFilter);
```

One value, by extension method:
```csharp
string selectedFilter = Request.Querstring["filter"];
query = query.Filter(p => p.GetDepartmentID(), selectedFilter);
```

Multiple values:
```csharp
string[] selectedFilters = Request.Querstring["filters"];
query = query.Filter(p => p.DepartmentID, selectedFilters);
```

Use AND operator to filter on all filters:
```csharp
string[] selectedFilters = Request.Querstring["filters"];
query = query.Filter(p => p.DepartmentID, selectedFilters, Operator.And);
```


## Fetch the results
```csharp
SearchResult results = query.GetResults();
```


## Retrieve facets
```csharp
foreach (FacetEntry facet in results.Facets)
{
    // facet.Key = name of the property, ie. DepartmentID
    // facet.Count = number of unique values for this facet

    foreach (FacetHit hit in facet.Hits)
    {
        // hit.Key = facet value
        // hit.Count = number with this value
    }
}
```



## Advanced filtering

Multiple filters can be grouped to form more complex queries.

In the below example, the following must be true to give a match:

`Sizes` is either `"xs"` or `"xl"`  
`ProductCategory` is `"pants"`  
`Brand` is either `"levis"` or `"diesel"`  

```csharp
query = query
    .FilterGroup(group => group
        .Or(page => page.Sizes(), new[] { "xs", "xl" }) 
        .And(page => page.ProductCategory, "pants") 
        .Or(page => page.Brand, new[] {"levis", "diesel"}) 
    );
```

&nbsp;

## Not-filter

To filter *away* certain values, use `FilterMustNot`

```csharp
var query = service
   .Search<PageData>("foo")
   .FilterMustNot(x => x.Title, "bar");
```
&nbsp;

# ACL

To filter on the current users ACL, use `FilterByACL`

```csharp
var query = service
   .Search<PageData>("foo")
   .FilterByACL();
```

`EPiServer.Security.PrincipalInfo.Current` will be used by default, but a custom `PrincipalInfo` can be supplied if needed. 

&nbsp;


# Range
To search within a given range of values, use the `Range` function.  
Supported types are `DateTime`, `double`, `decimal` and `long` (including implicit conversion of `int`, `byte` etc.)


```csharp
SearchResult result = service
   .Search("bacon")
   .Range(x => x.StartPublish, DateTime.Now.Date, DateTime.Now.Date.AddDays(2))
   .GetResults();
```

```csharp
SearchResult result = service
   .Search("bacon")
   .Range(x => x.MyNumber, 10, 20)
   .GetResults();
```

The less-than argument is optional.

```csharp
SearchResult result = service
   .Search("bacon")
   .Range(x => x.MyNumber, 10) // Returns anything above 10
   .GetResults();
```

To search for an interval inside another interval, your property must be of type `Epinova.ElasticSearch.Core.Models.Properties.IntegerRange`.  
Currently `int` is the only supported type.

```csharp
SearchResult result = service
   .Search("bacon")
   .Range(x => x.MyRange, 10, 20)
   .GetResults();
```

```
public class ArticlePage : StandardPage
{
    [Display]
    public virtual int From { get; set; }

    [Display]
    public virtual int To { get; set; }

    public IntegerRange MyRange => new IntegerRange(From, To);
}
```

# Dictionary properties
If a property is of type `IDictionary<string, object>` and marked `[Searchable]`, it will be indexed as an `object` in Elasticsearch.  
This is useful in scenarios where you have dynamic key-value data which must be indexed, like from PIM-systems.  

Standard property approach:

```
public class ProductPage
{
    [Searchable]
    public IDictionary<string, object> Metadata { get; set; }
}
```

The data itself will not be returned, but you can query it and make facets:

```csharp
SearchResult result = service
   .Search<ProductPage>("bacon")
   .InField(x => x.Metadata + ".SomeKey")
   .FacetsFor(x => x.Metadata + ".SomeKey")
   .GetResults();
```

Custom properties:

```csharp
public static class SearchConfig
{
    public static void Init()
    {
        Epinova.ElasticSearch.Core.Conventions.Indexing.Instance
            .ForType<ProductPage>().IncludeField("Metadata", x => x.GetPimDataDictionary());
    }
}
```

This approach returns the data:

```csharp
SearchResult result = service
   .Search<ProductPage>("bacon")
   .GetResults();

var hit = result.Hits.First();
var dict = hit.Custom["Metadata"] as IDictionary<string, object>
```



&nbsp;


# Boosting

Properties can be boosted by decorating them with the `Boost` attribute:
```csharp
[Boost(13)]
public string Title { get; set; }
```

… or at query time:
```csharp
SearchResult result = service
   .Search("bacon")
   .Boost(x => x.MyProp, 3)
   .GetResults();
```

A type can be given either a positive or a negative boost:

```csharp
SearchResult result = service
   .Search("bacon")
   .Boost<ArticlePage>(2)
   .GetResults();
```
```csharp
SearchResult result = service
   .Search("bacon")
   .Boost(typeof(ArticlePage), 2)
   .GetResults();
```
```csharp
SearchResult result = service
   .Search("bacon")
   .Boost<ArticlePage>(-3)
   .GetResults();
```

You can also boost hits depending on their location in the page tree:

```csharp
SearchResult result = service
   .Search("bacon")
   .BoostByAncestor(new ContentReference(42), 2)
   .GetResults();
```

Date properties can be scored lower depending on their value, by using the `Decay` function. This way you can promote newer articles over older ones.

```csharp
SearchResult result = service
   .Search("bacon")
   .Decay(x => x.StartPublish, TimeSpan.FromDays(7))
   .GetResults();
```

Every time the date interval (2nd argument) occurs, 0.5 points will be subtracted from the score.
In the example above, 0.5 points will be subtracted after 7 days, 1 points after 14 days, and so on.

&nbsp;


# Best bets

Use `.UseBestBets()` on your query to score selected content higher than normal. 

```csharp
SearchResult result = service
   .Search("bacon")
   .UseBestBets()
   .GetResults();
```

&nbsp;

Best bets can be administered via the embedded menu Search Engine -> Best Bets.
&nbsp;

![Tools](assets/bestbets.png?raw)

&nbsp;



# Tracking

Simple statistics can be collected using the `.Track()` function. This will track the number of times a term is queried and whether it returned any hits.

```csharp
SearchResult result = service
   .Search("bacon")
   .Track()
   .GetResults();
```

&nbsp;

![Tools](assets/tracking.PNG?raw)


*Note: If your connection string is not named `EPiServerDB` you must supply its name in the `trackingConnectionStringName`-configuration See [Installation](setup.md)*

&nbsp;

# Stemming

Stemming is by default applied to all properties of type `XhtmlString`, or those named `MainIntro` or `MainBody`.  
The language is based on the content language. Other properties of type `string` can be stemmed by decorating them with the `Stem`-attribute:

```csharp
[Stem]
public string Title { get; set; }
```

&nbsp;

# Listing contents
To list contents of a certain type without any scoring or analysis, use the `Get` function. 
This can be used in conjunction with `SortBy` for simple listings.

```csharp
SearchResult result = service
   .StartFrom(somePageLink)
   .Get<ArticlePage>()
   .GetResults();
```

&nbsp;


# Sorting
Sorting is normally performed by Elasticsearch based on the score of each match. 
Manual sorting should only be used in scenarios where scoring is not relevant, e.g. when using the previously mentioned `Get` function. 

```csharp
SearchResult result = service
   .Get<ArticlePage>()
   .SortBy(p => p.StartPublish)
   .ThenBy(p => p.Foo)
   .ThenBy(p => p.Bar)
   .GetResults();
```

&nbsp;

#### Geo-points

When sorting on a GeoPoint, there is one more mandatory argument; `compareTo`. 
Items will be compared to these coordinates, and the resulting distances will be used as the sort values.

&nbsp;

#### Scripted sorting
For absolute control you can use a script to sort documents. Note that this might affect performance.

Example sorting based on a certain timestamp:

```csharp
var timestamp = new DateTimeOffset(new DateTime(2019, 1, 1)).ToUnixTimeMilliseconds();
var script = $"doc['StartPublish'].date.getMillis() > {timestamp} ? 0 : 1";

SearchResult result = service
   .Get<ArticlePage>()
   .SortByScript(script, true, "number")
   .GetResults();
```

See https://www.elastic.co/guide/en/elasticsearch/painless/current/index.html for scripting syntax.

&nbsp;

# &laquo;Did You Mean&raquo;

A shingle filter is included that can suggest similar words found in the index when searching for misspelled words.  
Any suggestions found will be included in the `DidYouMean` property of the search result:

```csharp
SearchResult result = service
   .Search("alloi")
   .GetResults();

string[] didYouMean = result.DidYouMean; // [ "alloy", "all" ]
```

Any properties that should act as a source for suggestions must be marked with `[DidYouMeanSource]`.

```csharp
public class StandardPage : SitePageData
{
    [DidYouMeanSource]
    public virtual XhtmlString MainBody { get; set; }
}
```

&nbsp;


# Episerver specifics

## GetContentResults
The results returned by `GetResults()` does not have any knowledge of Episerver. Use the function `GetContentResults()` in an Episerver context.  
This will automatically apply the filters `FilterAccess`, `FilterPublished` and `FilterTemplate`.

```csharp
IEnumerable<IContent> content = service
   .Search<CoursePage>(text)
   .GetContentResults();
```
&nbsp;



## Search in edit-mode

If you want to use any of the included providers when searching in edit mode, go to CMS -> Admin -> Config -> Tool settings -> Search Configuration.  
Tick the providers and drag them to the top of the list. 

## Synonyms

Synonyms can be administered from the menu Search Engine -> Synonyms.  


## Re-indexing content

Content will be automatically re-indexed when performing common operations such as publishing, moving and deletion.

&nbsp;

To do an initial indexing of all contents, run the scheduled task &laquo;Elasticsearch: Index CMS content&raquo;

&nbsp;

Re-indexing can also be triggered manually on individual content via the Tools-menu:

![Tools](assets/tools-button.png?raw)

&nbsp;

… or via the context menu in the page tree:

![Tools](assets/tree-button.png?raw)

&nbsp;

# Episerver Commerce specifics

## GetCatalogResults 
The results returned by `GetResults()` does not have any knowledge of Episerver. Use the function `GetCatalogResults()` in an Episerver Commerce context. 
This will automatically choose the correct index and apply the filters `FilterAccess`, `FilterPublished` and `FilterTemplate`.

```csharp
IEnumerable<ProductContent> content = service
   .Search<ProductContent>(text)
   .GetCatalogResults();
```
&nbsp;

## Re-indexing content

Content will be automatically re-indexed when performing common operations such as publishing, moving and deletion.

&nbsp;

To do an initial indexing of all contents, run the scheduled task &laquo;Elasticsearch: Index Commerce content&raquo;

&nbsp;


# N-gram / Tri-gram tokenizer

You can switch between normal and tri-gram tokenizer (hardcoded to min=3, max=3, tokens=digit,char, per Elastic recommendations) 
via the menu Search Engine -> Administration and status.  

&nbsp;



# Highlighting / Excerpt

Use the `Highlight()` function to get an excerpt of 150 characters from where the match occurred in the text. 

```csharp
SearchResult result = service
   .Search("bacon")
   .Highlight()
   .GetContentResults();
```

Highlighting is by default enabled on properties named `MainIntro`, `MainBody`, `Attachment` and `Description`.  
Default marker is `<mark>`

You can customize this behaviour with the following configuration options:
```csharp
Indexing.Instance.ForType<ArticlePage>().EnableHighlighting(x => x.MyField);
Indexing.Instance.SetHighlightFragmentSize(42);
Indexing.Instance.SetHighlightTag("blink");
```

The results can be found in the `Highlight` property of each hit.

&nbsp;

# Custom types (non-Episerver content)

Use the `Bulk` function to index custom content. 

Example:
```csharp
var obj1 = new ComplexType { StringProperty = "this is myobj 1" };
var obj2 = new ComplexType { StringProperty = "this is myobj 2" };
var obj3 = new ComplexType { StringProperty = "this is myobj 3" };

ICoreIndexer indexer = ServiceLocator.Current.GetInstance<ICoreIndexer>(); // Can also be injected

BulkBatchResult bulk = indexer.Bulk(new[]
{
    new BulkOperation(obj1, "no"),
    new BulkOperation(obj2, "no"),
    new BulkOperation(obj3, "no")
});

var results = _service
    .Search<ComplexType>("myobj")
    .InField(x => x.StringProperty)
    .GetCustomResults();
```

## Custom index

If you prefer a custom index to avoid e.g. collisions, this must be supplied when indexing and searching:

```csharp
var obj1 = new ComplexType { StringProperty = "this is myobj 1" };
var obj2 = new ComplexType { StringProperty = "this is myobj 2" };
var obj3 = new ComplexType { StringProperty = "this is myobj 3" };

ICoreIndexer indexer = ServiceLocator.Current.GetInstance<ICoreIndexer>(); // Can also be injected

string indexName = "my-uber-custom-name-no";

BulkBatchResult bulk = indexer.Bulk(new[]
{
    new BulkOperation(obj1, "no", index: indexName),
    new BulkOperation(obj2, "no", index: indexName),
    new BulkOperation(obj3, "no", index: indexName)
});


var results = _service
    .UseIndex(indexName)
    .Search<ComplexType>("myobj")
    .InField(x => x.StringProperty)
    .GetCustomResults();
```

## Set custom http client message handler

Epinova.ElasticSearch uses standard `HttpClient` to call elasticsearch. Sometimes it's neccassary to handle messages sent differently. For example signing request for cloud services.

If you want several HttpMessageHandlers, we recommend chaining them before setting.

For Example:
```csharp
MessageHandler.Instance.SetMessageHandler(new AWSHandler());
```

&nbsp;

## Important
`GetCustomResults` returns strongly typed objects as opposed to
`GetContentResults`, which only returns IDs.

Custom objects do not require an `Id` property (or corresponding argument in the `BulkOperation` ctor), but this is recommended if you want control over versioning and updates/deletions.

&nbsp;

# See also
* [Installation](setup.md)
* [Changelog](changelog.md)
