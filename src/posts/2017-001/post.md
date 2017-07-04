---
title: "Trench Talk: Identity"
slug: "trench-talk-identity"
date: 2017-07-04
author: Yves Reynhout
publish: true
---
In his book [_"Analysis Patterns, Reusable Object Models"_](https://martinfowler.com/books/ap.html) Martin Fowler discusses the idea of object identity, i.e. conceptual references to objects. One of the easiest way to refer to an object is by giving it a _name_. Martin describes a name as an informal identifier of an object. I'm sure if you've ever built one of those _CRUD_ screens that help set up data to be used at the operational level, giving a name to things sounds familiar. Master data management, administrative settings, configuration settings, setup, knowledge level data are but a few of the terms I've heard people use to refer to these type of screens. Some even manipulate these using DBMS tooling directly. Inevitably a requirement will pop up to make those names unique. Why? Because of the ambiguity that may arise at the operational level if names are used to refer to objects. To illustrate, if you have a drop down with multiple occurrences of the same name, which one should you pick? One unexpected side effect of introducing uniqueness constraints is that end users - assuming they have access - get really creative in pre- or suffixing these names with e.g. organizational unit names as to not clash. Clearly an indication you may have missed something in your original design, although that point is debatable.
Now, obviously, the same object can very well have multiple names or identifiers associated with it. To differentiate between those and to proactively disambiguate, Martin suggests adding an identification scheme that acts a context used to identify an object. The advantage is that the same identifier could exist in multiple schemes without any chance of collision. That is, if we enforce uniqueness within that context. Personally, I find this very practical when building integrations with multiple systems where I may not be able to impose my identity onto the other system or the same identity across all systems. Such a map between the internal and external identification often sits at the edges, close to the integration points. One piece of advice that really stood out for me was the following:

> A true identifier reliably leads the user to one and only object and must always lead to the same object whenever it is used.

That effectively makes an identifier immutable in space and time, inherently prohibiting reuse. Anybody familiar with event sourcing or event driven approaches will enjoy the immutability aspect. The similarity with e.g. a [URN](https://tools.ietf.org/html/rfc8141) is striking. It's not that difficult to find practical examples of identifiers and identification schemes that are in use in a given domain. For example, in banking you have IBAN and BIC (SWIFT), to name a few. Given the importance of messaging in today's world, having a firm grasp of how object identification works is not a luxury, in my opinion. That said, the most common and simple form of object identification developers encounter is the use of primary keys in a relational  setting. 

# It's complicated

I didn't just ramp up to this point to merely talk about how you map identifiers to objects. In today's distributed services world, whether yours is of the micro, service orientation, event driven, streaming or bounded context kind, distributing object identifiers is a given. With that comes a fairly insidious problem. What if the humans that enter data make mistakes and duplicate information? Imagine, a hospital system that has, as one of its services, a [master patient index](https://en.wikipedia.org/wiki/Enterprise_master_patient_index) that provides a consistent, accurate and current (sometimes even historic) view on patients. The motivation for having a single unique patient identifier is to make sure a patient is represented only once across all the other services in a hospital (a boundary that may be somewhat artificial in the modern world). Despite its best efforts, duplicates may enter the index due to human error, odd ways in which legacy systems force interaction with the service, ambiguous or unknown patient identity at the time of registration, etc ... Meanwhile other services are collecting data of their own using this _assumed to be unique_ patient identifier, which in reality it is not. At least, not always. It may be weeks or months before this duplication is spotted. In case of health care, where decisions are made based on who you are and attributes associated with you (e.g. gender, age, insurance, medical history), having an accurate patient representation is not some hollow concept. So, how do we keep our sanity? In his book, Martin proposes 3 strategies, namely:

- copy-and-replace: all the properties of the one object are copied over to a chosen object, after which we remove all references to the copied object and remove the copied object itself. Obviously, this violates the immutability aspect and is rather destructive. It's also prone to missing a reference to the copied object.
- superseding: a less destructive approach whereby one object is classified as being superseded by and thus linked to another, active object. The superseded object is kept for historical reasons and there's no reason to replace references in this case. Any future change requests to the superseded object are forwarded to the active object. Any future query requests must consider both the superseded and active objects. Data can potentially be copied over to the active object, making it easier to support future requests.
- essence-appearance: here, the object remains pretty much the same, but sitting behind it is another object, the essence, whose sole responsibility is to link multiple objects together. It has no other properties which implies that data is not copied to the essence. It's probably the easiest strategy in case the linking of objects needs to be undone.

> Please bare in mind this book came out in 1996 and talks about projects Martin worked on in the late 80's, early 90's. The wording may be a bit dated and geared towards the Object Oriented paradigm. Nevertheless, it's still useful when it comes to solving the problem in a distributed setting.

Now, when we look at this through the lense of service orientation, the copy and replace strategy may be used within the service boundary. Yet as soon as you hand out identifiers - outside of the service boundary, you give up control of what may know about and remember an identifier, making it a less than ideal strategy. The superseding strategy comes in two flavors. One is to apply the superseding within the service boundary, alleviating the other services from having to worry about what happens within the service. The other is to actively involve the other services, providing them with a constant stream of messages that indicate which object was superseded by which other object, in essence allowing them to come to their own conclusions. Trade offs have to be made when choosing between these two flavors. The essence-appearance strategy is rather similar to superseding. The main difference is one of emphasis, either on linking and unlinking objects in the essence-appearance case or on one object being superseded by another, active object in the superseding case. If you don't have a problem that fits these solutions, it may come across as splitting hairs.

Imagine a virtual wallet handed out to a player in an online casino, modelled as a [source of events](http://docs.geteventstore.com/introduction/4.0.0/event-sourcing-basics/). After a while the accumulation of events may make the wallet painful to work with. The usage of a [snapshot](https://en.wikipedia.org/wiki/Memento_pattern) may alleviate the reading pressure but we're still stuck with a rather long stream of events that have been built up since the player first joined. What if we exchanged the player's wallet for a new one? He'd basically start of with a new wallet that had an initial balance and a soft reference to his old wallet. Essentially, we've just applied the superseding strategy, albeit in a domain specific way. Simplistically the messages could be described as:

```fsharp
type WalletWasHandedOut = 
    { 
        PlayerId: string;
        WalletId: string;
        InitialBalance: decimal;
        When: long;
    }

type WalletWasExchanged = 
    { 
        OldWalletId: string; 
        NewWalletId: string;
        InitialBalance: decimal;
        When: long;
    }

```

Getting back to the example of the master patient index, imagine a scheduling service where appointments are booked for patients. This scheduling service would listen to messages from the master patient index that indicate when two patients identifiers are linked together or are no longer linked together, thereby linking or unlinking appointments. Any other service that would come and ask the scheduling service for the appointments of a particular patient identifier would get an accurate list based on what the scheduling service was told by the master patient index. Here, we've applied the essence-appearance strategy.

# Conclusion

There are countless examples that fit these patterns and given the rise or rebirth - depending on how you look at it - of services, they will become even more important once developers realize and understand that they need to embrace identity and duplication as part of their modeling and design exercise. Even more so if the tenets of service orientation are to be upheld. Older books often carry knowledge that is still relevant, even if it's described in terms that have fallen out of fashion or are no longer in vogue. 