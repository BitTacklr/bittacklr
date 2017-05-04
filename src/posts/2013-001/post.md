---
original: https://seabites.wordpress.com/2013/06/09/event-enrichment/
title: "Event Enrichment"
slug: "event-enrichment"
date: 2013-06-09
author: Yves Reynhout
publish: true
---
Event or more generally message enrichment, the act of adding information to a message, comes in many shapes and sizes. Below I scribbled down some of my thoughts on the subject.

Metadata
--------

*Meta*data, sometimes referred to as out of band data, is typically represented separately from the rest of the payload. Reusing the underlying protocol/service is the sane thing to do. [HTTP](http://en.wikipedia.org/wiki/Hypertext_Transfer_Protocol "Http Protocol"), [SOAP](http://www.w3.org/TR/soap/ "The SOAP Specifications"), [Amazon AWS](http://aws.amazon.com/ "Amazon Web Services")/[Windows Azure](http://www.windowsazure.com/en-us/ "Windows Azure") API calls, [Event Store](http://geteventstore.com/ "The Event Store")'s event metadata, etc ... are just a few examples of how metadata could be carried around next to the payload. Other examples are explicit envelopes or message wrappers, such as the Transmission Wrapper and the Intermediate Control Act Wrapper in the HL7 V3 specification. From a code perspective (and from my experience) the addition of metadata tends to happen in one place. Close to the metal boundary if you will, e.g. http handlers, message listeners, application services, etc ...

Separate concerns
-----------------

Sometimes you need to *augment* a message with an extra piece of data, but it feels more natural to make another piece of code responsible for adding that data. A typical example I can think of are things that have a name but you only have an identifier to start with. In an event-sourced domain model(\*), think of an aggregate that holds a soft reference (an identifier if you will) to another aggregate, but the event - about to be produced - would benefit from including the name of the referenced aggregate at that time. Many events may fall in the same boat. Having a dedicated piece of code doing the enrichment could make things more explicit in your code. Other times you may have a model that is computationally intensive, and adding the *extra* data would result in [sub optimal data-access](http://en.wikipedia.org/wiki/Big_O_notation#Orders_of_common_functions "Big O Notation"). How so? Well, each operation would require say a query, possibly querying the same data over and over again. While caching could mitigate some of this, having another piece of code do the enrichment could allow you to deal with this situation more effectively (e.g. batching). Not only that, but it could also make it very explicit what falls into to the category of enrichment and what not. This nuance may become even more important when you're working as a *team*. Sometimes the internal shape of your event may not be the external shape of your event, meaning they may need less, more, or even different data for various consumers. Whether such a transformation really is an enrichment is debatable. But assuming it is, it's just another one of those situations where enrichment makes sense. An advantage of doing enrichment separately is that certain things never enter your model. As an example, an aggregate in an event-sourced domain model, does it really need the version it is at or its identity(\*\*)? Could I not tack that information onto the related events as I'm about to persist them? Sure I can. No need to drag(\*\*) those things into my model. The enrichment could be implemented using an *explicit event enricher* that runs either synchronously in your event producing pipeline or asynchronously, depending on what makes the most sense. Using [event builders](http://seabites.wordpress.com/2013/05/30/eventbuilders-revisited/ "Event Builders - Revisited") has proven to be a killer combo. 

```csharp
 public class BeerNameEnricher : IEnrich&lt;BeerRecipeDescribed&gt;, IEnrich&lt;BeerRecipeAltered&gt; { Dictionary&lt;string, string&gt; \_lookupNameOfBeerUsingId; public BeerNameEnricher(Dictionary&lt;string, string&gt; lookupNameOfBeerUsingId) { \_lookupNameOfBeerUsingId = lookupNameOfBeerUsingId; } public BeerRecipeDescribed Enrich(BeerRecipeDescribed @event) { return @event.UsingBeerNamed(\_lookupNameOfBeerUsingId\[@event.BeerId\]); } public BeerRecipeAltered Enrich(BeerRecipeAltered @event) { return @event.UsingBeerNamed(\_lookupNameOfBeerUsingId\[@event.BeerId\]); } } 
```

 (\*): Familiarity with Domain Driven Design is assumed. (\*\*): Not every situation warrants this.
