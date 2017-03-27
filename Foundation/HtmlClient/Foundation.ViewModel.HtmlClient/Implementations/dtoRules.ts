module Foundation.ViewModel.Implementations {

    export class DtoRules<TDto extends Model.Contracts.IDto> {

        private _model: TDto;

        public get model(): TDto {
            return this._model;
        }

        public set model(value: TDto) {
            this._model = value;
        }

        public validateMember(memberName: keyof TDto, newValue: any, oldValue: any): void {
            let modelIsValid = this.model.isValid();
            let type = this.model.getType();
            let memberDefs = type.memberDefinitions;
            if (memberDefs[`$${memberName}`].required == true) {
                this.setMemberValidaty(memberName, "required", modelIsValid == true || this.model["ValidationErrors"] == null || this.model["ValidationErrors"].find(v => v.Type == 'required' && v.PropertyDefinition.name == memberName) == null);
            }
            if (memberDefs[`$${memberName}`].regex == true) {
                this.setMemberValidaty(memberName, "pattern", modelIsValid == true || this.model["ValidationErrors"] == null || this.model["ValidationErrors"].find(v => v.Type == "regex" && v.PropertyDefinition.name == memberName) == null);
            }
        }

        public setMemberValidaty: (memberName: keyof TDto, errorKey: string, isValid: boolean) => void;

        public async onActivated(): Promise<void> {

        }
    }

}