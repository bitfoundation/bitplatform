module Foundation.ViewModel.Implementations {
    @Core.ObjectDependency({ name: "GuidUtils" })
    export class GuidUtils {

        public newGuid(): string {

            return $data.Guid['NewGuid']().value.toLowerCase();
            
        }
    }
}