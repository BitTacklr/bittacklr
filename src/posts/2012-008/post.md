---
original: https://seabites.wordpress.com/2012/10/12/object-inheritance/
title: "Object Inheritance"
slug: "object-inheritance"
date: 2012-10-12
author: Yves Reynhout
publish: true
---
When mentioning "object inheritance" most people immediately think of "[class inheritance](http://en.wikipedia.org/wiki/Inheritance_(object-oriented_programming) "Class inheritance")". Alas, that's not what it is. Quoting from [Streamlined Object Modeling](http://www.amazon.com/Streamlined-Object-Modeling-Patterns-Implementation/dp/0130668397 "Streamlined object modeling")\*:

> Object inheritance allows two objects representing a single entity to be treated as one object by the rest of the system.

Put a different way, where class inheritance allows for code reuse, object inheritance allows for data reuse.

### Application

Where could this be useful? Depending on the domain you are in, I'd say quite a few places. Whenever you replicate data from one object to another object, you should stop and consider if "object inheritance" could be applicable. While object inheritance does impose some constraints, it also saves you from writing reconciliation code to synchronize two or more objects (especially in systems that heavily rely on messaging). A common example I like to use is the one of a video title and a video tape (\*\*). From a *real world* point of view, a video tape has both the attributes of the tape itself and the title. Yet, if I modeled this as two separate objects, I run into trouble (a.k.a. synchronization woes). If I copy the title information to the tape upon *creation* of the tape, and I made an error in say the release date of the title, I now have to replicate that information to the "affected" tapes. Don't get me wrong, sometimes this kind of behavior is desirable, i.e. it makes sense from a business perspective. But what if it doesn't? That's where "object inheritance" comes in. To a large extent, it can cover the scenarios that a synchronization based solution can, **IF** constraints are met.

### Constraints

-   Localized data: "Object inheritance" assumes that the data of both objects lives close together. That might be a deal breaker for larger systems, or at least an indication that you'd have to consider co-locating the data of "both" objects.
-   Immutable data: One of the objects, the "parent", its data is immutable during object inheritance. From an [OOP](http://en.wikipedia.org/wiki/Object-oriented_programming "Object oriented programming") perspective that means you can only invoke [side-effect free functions](http://domaindrivendesign.org/resources/ddd_terms "Side effect free function") on a "parent".
-   "Parent" object responsibilities: the parent object contains information and behaviors that are valid across multiple contexts, multiple interactions, and multiple variations of an object.
-   "Child" object responsibilities: the child object represents the parent in a specialized context, in a particular interaction, or as a distinct variation.

The aforementioned book also states other constraints such as the child object exhibiting the parent object's "profile" (think interface), but I find those less desirable in an environment that uses CQRS. For more in-depth information, I strongly suggest you read it. It has a wealth of information on this very topic.

### Code

In its simplest form "object inheritance" looks and feels like [object composition](http://en.wikipedia.org/wiki/Object_composition "Object composition"). \[gist\]3880987\[/gist\] The composition can be hidden from *child object consuming code* behind a repository's facade as shown below. \[gist\]3880929\[/gist\] The gist of "object inheritance" is that the child object (the video tape) asks the parent (the video) for data or invokes side-effect free functions on the parent to accomplish its task.

### Lunch -&gt; Not Free

Whether you go down the route of "object inheritance" or (message based) "synchronization", you will have to *think* about object life-cycles (both parent and child). Sometimes it's desirable for the child to have a still view of the parent, a snapshot if you will. Other times you may want the child to always see the parent in its most current form. Children can even change parent during their life-cycle. Other children may want a more "controlled" view of the parent, rolling forward or backward based on their needs or context. You can get pretty sophisticated with this technique, especially in an event sourced domain model since it's very well suited to roll up to a certain point in time or a certain revision of a parent. In the same breath, I should also say that you can very well shoot yourself in the foot with this technique, especially if the composition is to be taking place in the user interface, in which case there's no point in using this. It's also very easy to bury yourself in the pit of abstraction when talking about "child" and "parent", so do replace those words with nouns from your domain, and get your [archetypes](http://www.petercoad.com/download/bookpdfs/jmcuch01.pdf "Archetypes") right. All in all, I've found this a good tool in the box, that plays nicely with aggregates, event-sourcing, even state-based models that track the concept of time. It's not something I use everyday, but whenever I did, I ended up with less code. YMMV. (\*) [Streamlined object modeling](http://www.amazon.com/Streamlined-Object-Modeling-Patterns-Implementation/dp/0130668397 "Streamlined object modeling") is a successor to Peter Coad's "[Modeling In Color](http://www.amazon.com/Java-Modeling-Color-With-UML/dp/013011510X "Modeling In Color")". A Kindle version of the former is available. (\*\*) I'm from the "cassette & video" ages.
