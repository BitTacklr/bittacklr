---
original: https://seabites.wordpress.com/2011/02/28/command-design-survey/
title: "Command Design Survey"
slug: "command-design-survey"
date: 2011-02-28
author: Yves Reynhout
publish: false
---
A while back, I did a command design survey to get a feeling as to how people were designing their commands in a CQRS environment. A total of 28 people participated in the survey (excluding myself, as I didn't want to bias the results in any way). I want to thank each one of you for taking the time to do so. Here are the results: [<img src="http://seabites.files.wordpress.com/2011/02/cmdsurvery_initializeq.png" title="How do you initialize your command properties?" class="aligncenter size-full wp-image-142" width="402" height="280" />](cmdsurvery_behaviorq.png) To the question "What else is special about your commands?" I got this compiled list:

-   Nothing
-   For now I use what ncqrs requires
-   They contain correlation id, user, role, time stamp, context reference
-   Use data annotations for validations. Commands are treated as value objects.
-   They are DTOs, no behaviour
-   Nothing, they are just standard nCQRS CommandBase implementations at the moment.
-   Simple, no behavior in commands, handlers may throw.
-   Validates them using a nsb message mutator on the sending side and a msg handler on the receiving side
-   Immutable, self describing, explicit
-   They are dead simple :)
-   The base class/interface contains an Id property of type Guid.
-   Getters/Setters to use the command as the ASP.NET MVC view model;nCQRS base class has a marker interface, so both really.;No constructor, so no exception from the constructor. Validation is done by ASP.NET MVC so the user can see the validation errors. I validate again in a command service interceptor, to catch base commands from other sources.
-   I have a ICommand.Validate method that gets called by the dispatcher (returns IEnumerable)

Come to your own conclusions.
