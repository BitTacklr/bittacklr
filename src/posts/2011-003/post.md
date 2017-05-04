---
original: https://seabites.wordpress.com/2011/07/24/how-do-you-control-time/
title: "How do you control time?"
slug: "how-do-you-control-time"
date: 2011-07-24
author: Yves Reynhout
publish: true
---
Granted, in some systems this might not be an issue. But if you want to be able to test your system's behavior in a deterministic fashion, it becomes important to be able to control time. So how do you control time in your code? Simple, you define your own Clock and use it instead of the System.DateTime static members. 

```csharp
 public static class Clock { static Func&lt;DateTime&gt; \_nowValueProvider; static Clock() { \_nowValueProvider = () =&gt; DateTime.Now; } public static void Initialize(Func&lt;DateTime&gt; nowValueProvider) { \_nowValueProvider = nowValueProvider; } public static DateTime Now { get { return \_nowValueProvider(); } } public static DateTime Today { get { return Now.Date; } } } 
```

 Now, I'm not gonna take credit for this code. It was authored by my coworker [Wouter Naessens](http://twitter.com/#!/wnaessens "Wouter Naessens") and inspired by an old [Ayende](http://twitter.com/#!/ayende "Ayende") post (<http://ayende.com/blog/3408/dealing-with-time-in-tests>). People using [NodaTime](http://code.google.com/p/noda-time/ "NodaTime") will call this a cheap rip-off (<http://code.google.com/p/noda-time/source/browse/src/NodaTime/Clock.cs>). If you search the net, you'll find other similar examples. Regardless of how you decide to implement this, what's important is that you do.

> Those that have a copy of [NDepend](http://www.ndepend.com/ "NDepend") can scan their code for usages of DateTime.Now, DateTime.Today, ... and fail the build if any of those are detected.

Now imagine you're using messaging and you want to test your system in a deterministic way. Setting that clock in unit tests is all fine and dandy, but what about rolling time forward (or backward if need be) between two consecutive commands sent to your system? Simple, you define a system command called SetClock. 

```csharp
 public class SetClock : IMessage { public DateTime Now { get; private set; } public SetClock(DateTime now) { Now = now; } } public class SetClockHandler : IHandle&lt;SetClock&gt; { public void Handle(SetClock message) { Clock.Initialize(() =&gt; message.Now); //If you want to keep time rolling forward //from message.Now onwards you'll have to be //more inventive. } } 
```

 You could also embed the clock's time as a header in the command's envelope, allowing a message handler to initialize the clock off the header value. There are lots of variations possible. Using this technique you should be able to replay use cases that involve time, or do acceptance/integration testing that involves time.
