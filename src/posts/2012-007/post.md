---
original: https://seabites.wordpress.com/2012/08/29/the-money-box/
title: "The Money Box"
slug: "the-money-box"
date: 2012-08-29
author: Yves Reynhout
publish: true
---
Once in a while I hear/read people struggle with [projection](http://cqrsguide.com/doc:projection "Projection") performance. The root causes of their performance issues are diverse:

-   using the same persistence behavior during projection rebuild as during live/production build
-   use of ill fit technology (here's looking at you [EF](http://msdn.microsoft.com/en-us/library/aa697427(v=vs.80).aspx "The Entity Framework")) or usage of said technology in the wrong way
-   too wide a persistence interface which makes it difficult to optimize for performance
-   no batching support or batching as an afterthought
-   not thinking about the implication of performing reads during projection
-   ...

Ever heard of the book *[Refactoring to Patterns](http://www.amazon.com/Refactoring-Patterns-Joshua-Kerievsky/dp/0321213351/ "Refactoring to patterns - the book")*? It has a nice refactoring in there, called *[Move Accumulation to Collecting Parameter](http://www.industriallogic.com/xp/refactoring/accumulationToCollection.html "Move accumulation to collecting parameter")* that refers to the *[Collecting Parameter](http://c2.com/cgi/wiki?CollectingParameter "Collecting parameter")* pattern on [C2](http://c2.com "C2 website"). How would this help with thy projections? Well, what if you could decouple the act of performing an action from collecting what is required to be able to perform an action? Put another way, what if you decouple the act of executing SQL DML statements from collecting those statements during projection (a.k.a. event handling)? So, instead of ... https://gist.github.com/3515704 ... we add another level of indirection ... https://gist.github.com/3515757 The most noticeable differences are the decoupling from persistence technology(\*), no reads, and no promises with regard to when the requested operations will be executed/flushed to storage. Usually, the IProjectionSqlOperations interface will have a very small surface (i.e. low member count), covering INSERT, UPDATE and DELETE. During live/production projection building you could have an implementation of this interface (a.k.a. [strategy](http://en.wikipedia.org/wiki/Strategy_pattern "The Strategy Pattern")) that flushes as soon as an operation is requested. However, the more interesting implementations are the ones that are used during rebuild. https://gist.github.com/3516020 This implementation translates the requested operations into sql statement objects (abstracted by ISqlStatement) and pushes them onto something that observes these sql statements. The observer couldn't care less what the actual sql statements are (that happened in the projection handler above). The simplest observer implementation could look something like this ... https://gist.github.com/3516121 Of course, collecting in and by itself is not all that useful. You have to do something with what you've collected ("the money in the box"). Let's look at another observer that takes a slightly different approach. https://gist.github.com/3516291 Without diving too much into the details, this observer flushes statements to the database as soon as a hard-coded threshold is reached. It does so in a batch-like fashion to minimize the number of roundtrips, but still adhering to the limitations that come with this particular ADO.NET data provider. Other implementations use SqlBulkCopy to maximize performance (but come with their own limitations). Depending on what memory resources a server has you could get pretty creative as to which strategy you choose to rebuild a large projection.

### Conclusion

I've shown you SQL centric projections, but please, do step out of the box. Nothing is stopping you from producing and collecting "HttpStatements" for your favorite key-value store or "FileOperations" for your file-based projections. Nothing is stopping you from making different choices for the producing and consuming side. Nothing is stopping you from doing the "statement" execution in an asynchronous and/or parallel fashion. It's just a matter of exploring your options and use what works in your environment. Next time I'll show you how reading fits into all this ... (\*): Yeah, yeah, I know, too much abstraction kills kittens ...
