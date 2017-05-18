---
original: https://seabites.wordpress.com/2014/11/10/reply-to-ddd-cqrs-es-lessons-learned/
title: "Reply to \"DDD, CQRS & ES: Lessons Learned\""
slug: "reply-to-ddd-cqrs-es-lessons-learned"
date: 2014-11-10
author: Yves Reynhout
publish: false
---
Last Monday I attended a meetup of the Belgian Domain Driven Design group. One of the [presentations](http://www.slideshare.net/Qframe/ddd-cqrs-es-lessons-learned "The slide deck") was titled *DDD, CQRS & ES: Lessons Learned*, which - what did you expect? - caught my interest. [Gitte](https://twitter.com/GitteTitter "Gitte's twitter"), the presenter, explained the problems she and her team came across while applying the aforementioned set of acronyms. None of these stumbling blocks came as a surprise to me. It's the same kind of issues that almost everyone faces when they first set out on this journey. The solutions are often trivial and diverse, but sometimes they're not and may require a bit of explanation. I didn't get a chance to discuss some of the alternatives with Gitte directly. You know how it goes, you end up talking to a lot of people but not everyone, and perhaps, in retrospect, it's better to let things sink in, instead of blurting out what can only come across as condescending "I know better" type of remarks, which, honestly, is not my intent. So without further ado, let's go over some of those problems:

Unique incremental numbers
--------------------------

### Description

The basic problem here is that they wanted to assign each *dossier* a unique number with the added requirement that it had to be incremental. She went on to explain that the read model (for user interface purposes) wasn't a good place, since that was just a side-effect of replaying events and would easily lose track of the highest number. Also the eventual consistent nature of that read model made it unsuitable for such purposes. In the end they settled on a singleton-eque, event-sourced aggregate that was made responsible for handing out unique numbers.

> As an aside, if Greg Young earned a dollar for the number of times the "uniqueness" problem was discussed on the [DDD/CARS mailing list](https://groups.google.com/forum/#!forum/dddcqrs "The DDD/CQRS mailing list"), he'd be able to buy a new laptop (I'm being modest).

### Reply

The problem with "uniqueness" requirements is that, well, very often there's a deeper underlying reason why people want them. In this case, I can't shake the feeling that it was a mixture of (a) having a human readable identifier that is easily communicated over the phone or in a letter, either among employees of the corporation or between employees and the customers/suppliers of the corporation and (b) knowing the number of *dossier* handled a year by just looking at the number. Obviously I'm going out on a limb in this particular case, but in general digging deeper in the domain gives a better understanding of why these requirements are in fact requirements. The aforementioned requirements could be solved in an entirely different manner than making an event-sourced number generator. Numbers could be assigned at a later point in time ordering all *dossier births* sequentially, or we could have a number reservation *service* (used quite liberal here) that hands out exclusive numbers and keeps a time-based lock on said number for us, or a high-lo reservation mechanism of numbers. But such efforts should be weighed against "real" requirements. What's the business impact of handing out numbers in a different order, or having temporary gaps in the set of numbers, or what if the numbers do not correspond to the chronological order of events? Who is losing sleep over this? Now, the most common response to this is "But, but, but this was dead easy when we used our relational model. The database generated incremental numbers for us". I'm sorry, but, but, but, you seem to confuse the requirement with the implementation at that point. Yes, you can leverage that implementation to satisfy the requirement, nothing wrong with doing that in your CRUD or anemic based model, but how aware are you of that underlying requirement? Did you even stop and think about it? Because that's what these acronyms just made you do ...

Akin to this problem is set based validation for which you can read [this piece of prior art](http://codebetter.com/gregyoung/2010/08/12/eventual-consistency-and-set-validation/ "Set based validation").

Refactoring
-----------

### Description

For some reason they've found refactoring to be cumbersome, i.e. pulling behavior and corresponding events out of a particular aggregate and moving it into one or more new aggregates. Implementation wise they were using a derivative of [Mark Nijhof's](https://twitter.com/MarkNijhof "Mark's twitter")[Fohjin](https://github.com/MarkNijhof/Fohjin "Fohjin").

### Reply

I don't think the intent was to refactor here. Refactoring, i.e. restructuring code on the inside without changing external behavior, is much easier using an event-sourced model, especially since tests can be written using messages instead of the nasty coupling to implementation I usually get to see. The reason you break out to new aggregates is due to new insights, either around consistency boundaries or concepts that were implicit before or concepts that were misunderstood (Hm ... how did this even pass analysis, design, coding, Q&A? The reality we live in, I guess). That's not refactoring to me, but I don't want to be splitting hairs either. Regardless, the problem remains. Yet, state based models have to deal with this issue as well. You bend your code to a new structure and you transform the data into a shape that fits the new structure. Hopefully, you've been collecting enough historical data to be able to provide sane input to that data transformation. State based models tend to "forget" historical facts because nobody deemed them to be important. Historical structural models are an even more painful experience I've blocked from my memory, so let's not go there. Granted, tooling is more pervasive for structural models (or should I say the data that underpins them). But it's not exactly a walk in the park either. It's always going to be work, either way. It's just different work.

One thing I noticed was that they were using a base class for their events (https://github.com/MarkNijhof/Fohjin/blob/master/Fohjin.DDD.Example/Fohjin.DDD.Events/DomainEvent.cs) with a property called "AggregateId". That's one source of trouble when you want to move events to another aggregate. Don't do that. Just call it what it is, e.g. a CustomerId or an OrderId or a DossierId. The aggregate an event was produced by should cater for the identification of the corresponding stream, not the event itself (though e.g. an attribute/annotation based approach could work as well). Don't get me wrong, you can copy all data you like into the event, including the aggregate id, the event id, the version. But I doubt it's wise to make it the basis of your infrastructure. At least, such has been my experience. I could make a similar argument for the version and event id.

Another remark that struck me as odd was the fact that *event migration is non-trivial*. Odd, since I've always perceived event streams as lightweight, easy to transform, merge or split. This should be dead easy. If it's not, you might want to start digging to see where the friction is coming from.

Da Big Fat Aggregate Cat
------------------------

### Description

Some of their aggregates were getting too big, resulting in too many lines of code, especially since decision and state changing behavior had been split into two different methods.

### Reply

Their future solution to this issue is looking into splitting up the decision making and state tracking in two different classes. The good news is, it's been done before, no need to reinvent [that wheel](https://github.com/Lokad/lokad-iddd-sample/tree/master/Sample/Domain/CustomerAggregate "Split decision making and state tracking"). The most common reasons for these obese aggregates is that they have too fine-grained behaviors, their behaviors take parameter lists that require scrolling off-screen, the consistency boundary is modeled after a *noun*, or we might be forcing an event-sourced approach onto what would be better served with a mixture of a document and events. It's hard to tell without looking at the specifics, but it's often an indicator that more investigation is needed instead of brushing it off as a code navigation issue. Vaughn Vernon's [book](http://idddworkshop.com/ "Implementing domain driven design") and [papers](http://dddcommunity.org/library/vernon_2011/ "Effective aggregate design"), as well as this excellent [post](http://gojko.net/2009/06/23/improving-performance-and-scalability-with-ddd/ "Scaling your aggregates") by Gojko Adzic should be a gentle reminder of what care and consideration should go into designing your aggregates.

Tooling
-------

### Description

Tooling for building these types of systems isn't as pervasive as it is for structural/state-based models. Be prepared to build a lot of it yourself.

### Reply

From an operational point of view there might be some truth to this statement and there's definitely room for improvement. But maybe that's what I love about it. It's more about cherry-picking existing tooling that fills the gaps that need filling. For the most part it's about understanding the mechanics and the forces at play. There's no wrong or right, there's just various trade-offs and specific requirements/needs. Trying to come up with something that satisfies a lot of the generic requirements would lead us down the path of the lowest common denominator or a Swiss army knife you configure using lots of xml, json, yaml or even code. Let's not go there ... ever again. I know, sounds like hand-waving, but give me a specific problem, and I'll try to give you a specific answer.

To put things into perspective, I've built a stream viewer/smuggler/transformer/rewriter on top of [NEventStore](https://github.com/NEventStore/NEventStore "NEventStore"), all in under one day's worth of work. Not production quality, but good enough nonetheless. Alternatively, I could have chosen to leverage a document store, put all the events in it using a catch-up subscription and reaped the benefits of full text searching those events, all in under a few hours, all because I know how to marry a paradigm and a set of technologies ... and this to me **is***the essential part*.

In the end ...
==============

I've just reiterated some of the problems they've encountered. It's good people discuss these things out in the open, because that's how you shorten the feedback loop, even if the solutions seem trivial or highly depend on context. I hope it's clear that my replies should not be interpreted as glorifying the acronyms nor as bashing of the presenter.
