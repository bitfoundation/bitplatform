module Foundation.ViewModel.Implementations {
    @Core.ObjectDependency({ name: "GuidUtils" })
    export class GuidUtils {

        public newGuid(): string {

            return $data.Guid['NewGuid']().value.toLowerCase();
            
        }

        public emptyGuid(): string {

            return "00000000-0000-0000-0000-000000000000";

        }
    }
}