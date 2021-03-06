---
original: https://seabites.wordpress.com/2015/04/08/trench-talk-projections/
title: "Trench Talk: Projections"
slug: "trench-talk-projections"
date: 2015-04-08
author: Yves Reynhout
publish: true
---
It's been two years since I first started working on [Projac](https://github.com/yreynhout/Projac "Projac"). At the time, I had an itch that needed scratching. Even before that time, I had written a fair amount of projections using an ORM (Entity Framework & NHibernate), using Dapper and a bunch of DTOs resembling a table's structure, using plain ADO.NET and using what can be classified as a predecessor of the ideas that landed and matured in Projac. It's safe to say that this experience has given me some insight in what I'm looking for and want from the projection authoring experience. I acknowledge, most of the work I've done in this area had revolved around projecting data into relational databases. That is, until recently. The past year or so I've added Elasticsearch, in-memory, and Windows Azure Blob Storage to the list of stores I project into. For fun, I tried things like Windows Azure Table Storage, Redis, Memcached and RavenDb to enrich my view on the respective store and to gain experience with their client APIs.

Birth of a projection
---------------------

How do you decide what store to project into? That decision will largely be driven by the capabilities of the store, the supported data types and/or structures in said store, and other non-functional requirements. What store best matches your query needs is a separate subject I'm not going to touch upon since there's enough information to go around about that topic. But even before that, why project at all? What is the driver for that decision? In regular LOB applications, it's often the UI or more specifically one or more views, that drive this decision. Even the write model could benefit from the occasional read model (the common term used to refer to the projected data) when it needs to ask a question along the way of executing a behavior (opinions differ in this case, but it remains a valid approach).

Once the need is identified, the next question becomes: what data should the projection contain? Most data will be there for showing. Some properties, key parts, columns (depends on your store) will be there to support filtering information based on user input or who that user is or the context in which the information is asked for. Other data will be there to guide or steer post processing of the data as a whole (e.g. enrichment based on an id). Yet another set of attributes might be there to enable future updates of the projected data (e.g. an id used to update an associated name). As you can tell, a lot of decisions go into this. The shape of the data returned by a query built on top of the projected data might be somewhat different from the stored projected data. Still, the reasons for those differences can most often be attributed to what I've described here.

A lot of the initial *design* discussions with regard to a projection revolve around the query/view needs: what data needs to be shown? how does it need to be filtered? how should it be sorted? etc ... For sure, the needs of the query api and projection will be somewhat intermingled, but that's to be expected since the projection's entire *raison d'être* is to support the query/view. Some untangling will be required before you're able to implement the projection. As the shape of the projection starts to take form, your attention will turn to how you're going to fill and maintain the projection: Which events affect the projection and how? This will bring about more attributes (if any) to be stored and how the projection actually happens. Intimate knowledge of the chosen store's update api and behavior is a big plus at this point. The set of events a projection *subscribes to* could be emitted by single stream (e.g. a single aggregate's stream or an aggregated/projected stream), a class of streams (e.g. for the same type of aggregate), cherry picked from a set of streams or what [EventStore](https://geteventstore.com/ "The EventStore") calls the *$all* stream. Thus, which stream or set of streams will provide the events is entirely driven by the needs of a projection, not the other way around.

To test or not
--------------

> Should I test my projection? Isn't that wasteful? I've heard they should be dead simple, so why test them? Even if I wanted to, how should I test them?

These are pesky questions, given that the answers only make sense within a certain context, namely your context. There's no universal answer, it's good old *IT DEPENDS*. Still, some observations can be made.

Why would you test a projection? Let me put it this way: Isn't it worthy to test the correctness of the information your end users are going to see? After all, that information is probably the basis for making a decision and invoking the next behavior in the system. Not the most convincing argument, but if it made you think, then mission accomplished. Assuming that you go along with my point of view that projections require testing, the next question becomes how? One thing that stands out is that this is *integration testing only*. There's simply no point in having an abstraction you can swap with something fake. You want the real thing, nothing less. Other than that, I've come across two schools of thought, namely testing a projection directly as a unit of isolation and testing a projection indirectly as part of the query api built on top. When testing a projection directly, you'd be using a syntax similar to this: 
```csharp
Given(events)
    .When(event)
    .Then(result)
```
where result is really the state of the projection - the verification process is store specific. The advantages in this case are that you're testing close to the SUT, i.e. the projection and that the number of test permutations will be rather low. The disadvantage is that you've only proven the correctness of the projected state, nothing more. When testing a projection indirectly, you'd be using a syntax similar to this: 
```csharp
Given(events)
    .When(request)
    .Then(response)
```
where the givens are what cause one or more projections to be filled. The SUT of these tests is different. It's on the query execution and the returned result. Quite possible that you'll end up with more test permutations since e.g. two different sets of givens could cause the same result to be returned for the same request. Not only that, but before you know it you'll bolt on authentication and authorization, client provided query steering variables, request and response representation format, etc ... That can become a lot, but I'll let you be the judge. So which one should I use? My answer would be both, since they serve a different purpose and you need them both to prove overall correctness. But again, don't take my word for it. By all means come to your own conclusions.

Units of isolation
------------------

Each projection stands on its own. It doesn't need another projection to do its job. If you deviate from this point of view, for good reasons perhaps, then you're entering the land of coupling and topological sorting. Good things come from isolation, such as the ability to replay just one projection and not having to worry about the other projections. So does that mean a projection can't have any dependencies? No, a stateless one seems fine to me. A stateful dependency could suffer from the same problems as depending on another projection. Be careful.

But it doesn't stop there. I tend to go one step further and treat any associated schema - obviously, this depends on the underlying store - to be part of that unit as well. True, it might not be everybody's cup of tea, but I kinda like it when both the data manipulation and the data definition projection code are close together.

> A common misconception is that a projection is a single table in a relational store. It isn't. It can be many tables you treat as unit and are filled by the projection. These tables shouldn't be shared across projections. A similar argument can be made for other stores if applicable.

Composition of data
-------------------

Sometimes events will lack information required by a projection. One way of dealing with this is having the projection listen for more events that do contain the required information. Doing it this way may involve tracking intermediate state not of particular interest to the query side but rather to be able to enrich the projection with data those events lack. An example would be a name associated with an identifier. If a future event only contains the identifier, for sure, an event must have come before it that does know the name that goes with the identifier. In a relational store I tend to call these cache tables and treat them as private to the projection (cfr. isolation). When the future event comes along, a simple lookup using the identifier in the cache table will return the name at that point in time. If a lot of the same information ends up in cache tables, it might be time to rethink your strategy and do the composition elsewhere (e.g. in the query layer, at the client or by virtue of event enrichment). Trade-offs will have to be made whichever way you decide to do it.

Change is inevitable
--------------------

Like any other piece of software, projections change due to changes in requirements. How you deal with change very much depends on non-functional requirements. Sometimes the answer will be to create a new *version* of the projection side-by-side to the old *version*, other times it'll be as part of a [blue/green deployment](http://martinfowler.com/bliki/BlueGreenDeployment.html "Blue Green Deployment"), or - if you can live with some downtime - a simple in-place upgrade might do the trick. The worst thing you can do is write something like *upgrade scripts* for your projections. Just don't. Stop and rethink the problem, because “it takes too much time to replay them” is not the cause, it's the symptom. Often the abuse of an unfit technology for the purpose of projecting and/or querying is to blame. Beyond that, not thinking about how data could further be partitioned along the axis of time, space, people, roles, etc ... is often at the heart of it all.

Conclusion
----------

Obviously, there's more to this subject than meets the eye. I've barely scratched the surface here - the various ways how events can get fed to a projection, declarative projections, single writer come to mind. As always my advice and experience should be taken with a grain of salt and not be copied like a cat would do. There's a time and place to bend the rules, but at least you're taking a conscious decision at that point.
