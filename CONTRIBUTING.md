# Contribution to bit platform

You can contribute to bit platform with issues and PRs. Simply filing issues for problems you encounter is a great way to contribute. Contributing implementations is greatly appreciated.

## Reporting Issues

We always welcome bug reports, API proposals and overall feedback. Here are a few tips on how you can make reporting your issue as effective as possible.

The bit platform codebase consists of multiple projects/products in a monorepo structure. Depending on the feedback you might want to file the issue with enough information to distinguish it among these projects/products.

### Finding Existing Issues

Before filing a new issue, please search our [open issues](https://github.com/bitfoundation/bitplatform/issues) to check if it already exists.

If you do find an existing issue, please include your own feedback in the discussion. Do consider upvoting (👍 reaction) the original post, as this helps us prioritize popular issues in our backlog.

### Writing a Good Feature request

Please provide enough information for the new feature so our contributors can understand the actual requirements. When ready to submit a proposal, please use the [Feature request issue template](https://github.com/bitfoundation/bitplatform/issues/new?assignees=&labels=&projects=&template=feature_request.yml).

### Writing a Good Bug Report

Good bug reports make it easier for maintainers to verify and root cause the underlying problem. The better a bug report, the faster the problem will be resolved. Ideally, a bug report should contain the following information:

* A high-level description of the problem.
* A _minimal reproduction_, i.e. the smallest size of code/configuration required to reproduce the wrong behavior.
* A description of the _expected behavior_, contrasted with the _actual behavior_ observed.
* Information on the environment: OS/distro, CPU arch, SDK version, etc.
* Additional information, e.g. is it a regression from previous versions? are there any known workarounds?

When ready to submit a bug report, please use the [Bug Report issue template](https://github.com/bitfoundation/bitplatform/issues/new?assignees=&labels=&projects=&template=bug_report.yml).

#### Are Minimal Reproductions Required?

In certain cases, creating a minimal reproduction might not be practical (e.g. due to nondeterministic factors, external dependencies). In such cases you would be asked to provide as much information as possible. If maintainers are unable to root cause of the problem, they might still close the issue as not actionable. While not required, minimal reproductions are strongly encouraged and will significantly improve the chances of your issue being prioritized and fixed by the maintainers.

#### How to Create a Minimal Reproduction

The best way to create a minimal reproduction is gradually removing code and dependencies from a reproducing app, until the problem no longer occurs. A good minimal reproduction:

* Excludes all unnecessary types, methods, code blocks, source files, nuget dependencies and project configurations.
* Contains documentation or code comments illustrating expected vs actual behavior.
* If possible, avoids performing any unneeded http calls.

## Contributing Changes

Project maintainers will merge changes that improve the product significantly.

### DOs and DON'Ts

Please do:

* **DO** follow our [coding style](docs/coding-style.md).
* **DO** give priority to the current style of the project or file you're changing even if it diverges from the general guidelines.
* **DO** include tests when adding new features. When fixing bugs, start with
  adding a test that highlights how the current behavior is broken.
* **DO** keep the discussions focused. When a new or related topic comes up.
  it's often better to create new issue than to side track the discussion.
* **DO** clearly state on an issue that you are going to take on implementing it.
* **DO** blog and tweet (or whatever) about your contributions, frequently!

Please do not:

* **DON'T** make PRs for coding style changes, instead discuss them in an issue.
* **DON'T** surprise us with big pull requests. Instead, file an issue and start
  a discussion so we can agree on a direction before you invest a large amount
  of time.
* **DON'T** commit code that you didn't write. If you find code that you think is a good fit to add to the bit platform, file an issue and start a discussion before proceeding.
* **DON'T** submit PRs that alter licensing related files. If you believe there's a problem with them, file an issue and we'll be happy to discuss it.
* **DON'T** add API additions without filing an issue and discussing with us first.

### Suggested Workflow

We use and recommend the following workflow which is explained in [this article](https://www.dataschool.io/how-to-contribute-on-github/):

1. Create an issue for your work.
    - Reuse an existing issue on the topic, if there is one.
    - Get agreement from the team and the community that your proposed change is a good one.
    - Clearly state that you are going to take on implementing it, if that's the case. You can request that the issue be assigned to you. Note: The issue filer and the implementer don't have to be the same person.
2. Create a personal fork of the repository on GitHub (if you don't already have one).
3. In your fork, create a branch off of develop.
    - Name the branch so that it clearly communicates your intentions, such as issue-123.
    - Branches are useful since they isolate your changes from incoming changes from upstream. They also enable you to create multiple PRs from the same fork.
4. Make and commit your changes to your branch.
    - [Build Instructions](docs/how-to-build.md) explains how to build and test.
    - Please use a good and informative commit message.
5. Add new tests corresponding to your change, if applicable.
6. Build the repository with your changes.
    - Make sure that the builds are clean.
    - Make sure that the tests are all passing, including your new tests.
7. Create a pull request (PR) against the **develop** branch.
    - State in the description what issue or improvement your change is addressing.
    - Check if all the Continuous Integration checks are passing. Check if any outstanding errors are known.
8. Wait for feedback or approval of your changes from the bit platform team.
9. When area owners have signed off, and all checks are green, your PR will be merged.
    - The next prerelease will automatically include your change.
    - You can delete the branch you used for making the change.

### Up for Grabs

The team marks the most straightforward issues as [up for grabs](https://github.com/bitfoundation/bitplatform/labels/up%20for%20grabs). This set of issues is the place to start if you are interested in contributing but new to the codebase.
