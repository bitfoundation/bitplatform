module Bit.Tests.ViewModels {

    @ComponentDependency({
        name: "App",
        templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/app.html"
    })
    export class App {

        public constructor( @Inject("MessageReceiver") public messageReceiver: Contracts.IMessageReceiver) {
        }

        @Command()
        public async $onInit(): Promise<void> {
            await this.messageReceiver.start();
        }

    }

}