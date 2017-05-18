---
original: https://seabites.wordpress.com/2013/11/26/trenchtalk-assertthatweunderstand/
title: "Trench Talk: Assert.That(We.Understand());"
slug: "trenchtalk-assertthatweunderstand"
date: 2013-11-26
author: Yves Reynhout
publish: false
---
After having written 2000+ eventsourcing-meets-aggregates specific *given-when-then* test specifications(\*), you can imagine I started to notice both "problems" and "patterns". Here's a small overview ...

> (\*) If you don't know what those are, here's a 50 minutes refresher for you: http://skillsmatter.com/podcast/design-architecture/talk-from-greg-young ... no TL;DW;

### Variations along the given and when axis

Ever written a set of tests where the when stays the same but the givens vary for each test case? Why is that? What are those tests communicating at that point? They're testing the same behavior (and its outcome) over and over again, but each time the [SUT](http://en.wikipedia.org/wiki/System_under_test "System under test") is in a (potentially) different state. With these tests, one particular behavior is put in the spotlight. When you are exploring scenarios - hopefully using a tangible, visual [DSL](http://en.wikipedia.org/wiki/Domain-specific_language "Domain specific language")**and** together with someone knowledgeable about the problem domain - you can uncover these variations. How? By gently steering the conversation in a direction where you keep asking what the outcome is of a certain behavior, each time changing the preconditions. Similarly, ever written a set of tests where the givens stay the same but the when varies for each test case? Why is that? They're testing different behavior, but each time the SUT is in the same state. From what I've seen, these tests are focusing on a particular state in the lifecycle of the SUT and constrain the behavior that can happen at that point in time. Again, in your exploratory conversations you can uncover these cases by focusing on a certain state and asking what the outcome of each behavior and/or behavior variation would be. Obviously, conversations don't always go the way you want them to, but I just wanted to point out the importance of language, and how listening attentively and asking the right questions enables you to determine appropriate input so you can assert that you've covered most - if not all - variations.

### Verbosity

Writing these tests, verbosity strikes in odd ways. It often becomes difficult for readers of the codified test specification to differentiate between the essential and secondary givens or thens. Both the "[Extract method](http://www.refactoring.com/catalog/extractMethod.html "Extract Method")" refactoring and the use of [test data builders](http://nat.truemesh.com/archives/000714.html "Test data builders") help a lot in reducing that verbosity and in bringing back the readability of the test. There's a parallel to this when generating printable/readable output based off of these test specifications for business people. Often this business oriented reader will not care for a lot of the minute details that are in the givens, when or thens. No, what he wants is a narrative that describes the specific scenario, emphasizing the important ingredients. So, how do we get that? Custom formatting. Not by overriding `.ToString()` on your messages, although you could still do that for other, more verbose purposes, but by associating a specific "narrative writer" with your message or scenario. I acknowledge this is pretty niche, but in my opinion it's important if you want to stick to *executable* specifications and not revert to *narrative on one side, code on the other side*. High coupling is a desired property in this area.

### Test data duplication

This may sound like it's the same as verbosity, but it isn't. It's related, yes, but not the same thing. I started noticing the same data(\*\*) being set up across a set of tests. Not a random set of tests, but a set of tests that focused on a certain feature, covered by one or more use cases. Basically, events and commands that were used in conjunction, with data that flowed from givens, over to when, into thens. Obviously, this is to be expected since it's the very nature of why one writes these tests. Now, you'd think that test data builders solve this problem. I'm inclined to say yes, if you allow them to be authored for a set of tests. That's a convoluted way of saying there are no general purpose test data builders that will work in each and every scenario. Now, you could move the variation that exists into those tests, but then you'd notice the duplication and, frankly, verbosity again. So, there's some "thing" sitting in between the test data builders and that set of tests. I call it the *Test Model*, abbreviated just *Model*. It captures the data, the events and commands, using methods and properties as I see fit, and is used in each of those tests. Some tests may put forward mild variations, either inline or as explicit methods, of existing events and/or commands, but that's okay. My gutt tells me I'm not at the end of the line here, but it's as far as I've gotten.

> (\*\*) I might be using data and messages interchangeably. I blame the wine.

### Isolation

This must be my favorite *advantage* of writing test specifications this way: the safety net that allows me to restructure things on the inside without my tests breaking because there is no coupling to an actual type. Refactoring on the inside does not affect any of the tests I've written using messages. I ***cherish*** this freedom enormously. Does that mean I don't write any unit tests for the objects on the inside? No, but I consider those to be more brittle. That's not a problem per sé, that's just something you need to be aware of. Now, how far does that safety net stretch, you might wonder? As long as you don't change the structure nor the semantics of the messages, I'd say pretty far. Again, these messages are capturing your assumptions and understanding, your conversation with that person that knows a thing or two about the business, better listen good, get them right and save yourself from a few *refuctors*.

### Dependencies

Although I haven't found much use for dependencies/services I needed to use on the inside, when I did, I found that this way of testing can work in full conjunction with mocking, stubbing, and/or faking. Why? Well, these dependencies are mostly important to the execution part of a specification, not its declaration. You can freely pass these along with the specification to the executor of the specification. The executor is responsible to make it available at the right place and the right time (\*\*\*).

> (\*\*\*) Remind me again what an inversion of control container does? ;-)

### Pipeline

Because the execution of test specifications is decoupled from its declaration, it's well suited to make sure you're not testing structurally invalid commands (or events if you want to go that far). Before executing your when, how hard would it be to validate that the command you're executing is actually structurally valid? If your command's constructor takes care of that you won't even be able to complete the declaration (\*\*\*\*). If you've made some other piece of code responsible for validating that type of command, then that's what is wellsuited to hook into the test specification execution pipeline. The pipeline is also suited to print out a readable form of the test specification as it is being executed.

> (\*\*\*\*) I have my reservations vis-à-vis said approach, but to each his own.

### Tests are data

At the end of the tunnel, this is the nirvana you reach. Why write these test specifications by hand when you can leverage that battalion of acceptance testers and end-users? Give them the tooling to record their scenarios. Sure, there's still value in capturing those initial conversations and those initial scenarios, but think of all the real world variations that end-users are generating on a day to day basis. To me, this is not some wild dream, this is the path forward, albeit littered with a few maintainability obstacles in my mind, but nothing I'm not willing to cope with. On a smaller scale, I found that if you leverage the test case generation features of your unit testing framework, you can already take baby steps in this direction. Think of the variations above. How hard would it be to consider either the set of givens or whens as nothing more than a bunch of test case data you feed into the same test? Think about how many lines of duplicate code that would save.

Conclusion
----------

So there you have it, an experience report from the trenches. Overall, I'm very "content" having written tests this way. I've made a lot of mistakes along the way, but that was to be expected. It should come as no surprise that many of these hard learned lessons defined the shape of [AggregateSource](https://github.com/yreynhout/AggregateSource "AggregateSource") and [AggregateSource.Testing](https://github.com/yreynhout/AggregateSource/tree/master/src/Testing "AggregateSource Testing").
