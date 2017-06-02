---
title: "Trench Talk: Identity"
slug: "trench-talk-identity"
date: 2017-05-31
author: Yves Reynhout
publish: false
---
In his book [_"Analysis Patterns, Reusable Object Models"_](https://martinfowler.com/books/ap.html) Martin Fowler discusses the idea of object identity, i.e. conceptual references to objects. One of the easiest way to refer to an object is by giving it a _name_. Martin describes a name as an informal identifier of an object. I'm sure if you've ever built one of those _CRUD_ screens that help set up data to be used at the operational level, giving a name to things sounds familiar. Master data management, administrative settings, configuration settings, setup, knowledge level data are but a few of the terms I've heard people use to refer to these type of screens. Some even manipulate these using DBMS tooling directly. Inevitably a requirement will pop up to make those names unique. Why? Because of the ambiguity that may arise at the operational level if names are used to refer to objects. To illustrate, if you have a drop down with multiple occurrences of the same name, which one should you pick? One unexpected side effect of introducing uniqueness constraints is that end users - assuming they have access - get really creative in pre- or suffixing these names with e.g. organizational unit names as to not clash. Clearly an indication you may have missed something in your original design, although that point is debatable.
Now, obviously, the same object can very well have multiple names or identifiers associated with it. To differentiate between those and to proactively disambiguate, Martin suggests adding an identification scheme that acts a context used to identify an object. The advantage is that the same identifier could exist in multiple schemes without any chance of collision. That is, if we enforce uniqueness within that context. Personally, I find this very practical when building integrations with multiple systems where I may not be able to impose my identity onto the other system or the same identity across all systems. Such a map between the internal and external identification often sits at the edges, close to the integration points. One piece of advice that really stood out for me was the following:

> A true identifier reliably leads the user to one and only object and must always lead to the same object whenever it is used.

That effectively makes an identifier immutable in space and time, inherently prohibiting reuse. Anybody familiar with event sourcing or event driven approaches should recognize the similarity with events. The similarity with a [URN](https://tools.ietf.org/html/rfc8141) is striking. It's not that difficult to find practical examples of identifiers and identification schemes that are in use in a given domain. Given the importance of messaging in today's world, having a firm grasp of how object identification works is not a luxury, in my opinion.

Complications
-------------



Copy and replace
Superseding
Essence /appearance

Conclusion
----------

