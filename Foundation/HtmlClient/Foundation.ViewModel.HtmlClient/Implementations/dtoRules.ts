module Foundation.ViewModel.Implementations {

    export class DtoRules<TDto extends Foundation.Model.Contracts.IDto> {

        private _model: TDto;

        public get model(): TDto {
            return this._model;
        }

        public set model(value: TDto) {
            this._model = value;
        }

        public validateMember(memberName: keyof TDto, newValue: any, oldValue: any): void {

        }

        public setMemberValidaty: (memberName: keyof TDto, errorKey: string, isValid: boolean) => void;
    }

}