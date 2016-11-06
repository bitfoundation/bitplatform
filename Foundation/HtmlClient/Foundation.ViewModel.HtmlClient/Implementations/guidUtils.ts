module Foundation.ViewModel.Implementations {
    @Core.ObjectDependency({ name: "GuidUtils" })
    export class GuidUtils {
        @Core.Log()
        public newGuid(): string {

            return $data.Guid['NewGuid']().value.toLowerCase();
            
        }
    }
}