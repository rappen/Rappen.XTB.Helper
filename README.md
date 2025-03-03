# [Wiki - all info and docs and links!](https://github.com/rappen/Rappen.XTB.Helper/wiki)


*Why **"XTB"**? 
This started from what I needed for the XrmToolBox tools, but it has been growing into separate features, see the wikis to understand why!*

This is used in all of my tools for XrmToolBox, and in Plugins, etc...

Try it!

# What is a RappSack?? ![RappSack](Rappen.XRM.RappSack/Images/RappSack_sqr_tsp_150px.png)

I have created a bunch of "base classes" over time - sometimes including too much, sometimes too simple.
I've called them xxxBag, xxxContainer, xxxUtils etc. etc.

I wanted to create base classes ND helpers for the current way we work with Dataverse, and the hardest thing is, of course, to find a proper name for it.
I want to have an easy name that explains what it does, a thing that contains an IOrganizationService, somewhere to log it, and might have info from the context... Trying to open my mind, letting ms Copilot help me.
Bag, Purse, Container, Sack, Grip...

A 'sack' is a part of a 'rucksack'... I like to use a rucksack, which is easy to carry and great for having everything I need in my backpack. I've used it forever; I never use a briefcase.

**RappSack** is my way of keeping all we need.<br/>
There is a `RappSackCore` that handles the most, an abstract class that implements the `IOrganizationService` and handles any type of logging.<br/>
The `RappSackPlugin` and `RappSackConsole` inherited the `RappSackCore`.<br/>
The `RappSackTracerCore` helps us to log stuff to where it can be stored. In plugins to the `ITracingService`, for console apps to a file and to the console, for Azure Functions to the general ILogger which shows up in Azure portal.<br/>
Then there are some other classes, most just `static`, like `RappSackMeta`, `RappSackUtils`.

Curious? Go look: https://jonasr.app/rappsack-code
