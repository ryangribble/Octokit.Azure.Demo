# Octokit.Azure.Demo

This is a demonstration of a .NET Core azure function that uses [Octokit.net](https://github.com/octokit/octokit.net) to respond to a GitHub WebHook

When receiving an `issues` `opened` webhook event, the function will
* Label the issue as `to_be_reviewed`
* Comment on the issue
