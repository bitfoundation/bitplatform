module Foundation.ViewModel.ViewModels {

    export class DtoRules<TDto extends Foundation.Model.Contracts.IDto> {

        private _model: TDto;

        public get model(): TDto {
            return this._model;
        }

        public set model(value: TDto) {
            this._model = value;
        }

        public validateMember(memberName: string, newValue: any, oldValue: any): void {

        }

        public setMemberValidaty: (memberName: string, errorKey: string, isValid: boolean) => void;
    }
}