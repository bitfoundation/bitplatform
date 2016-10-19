module Foundation.ViewModel.ViewModels {
    export class DtoViewModel<TDto extends Foundation.Model.Contracts.IDto, TRules extends DtoRules<TDto>>{

        private _model: TDto;

        public get model(): TDto {
            return this._model;
        }

        public set model(value: TDto) {
            this._model = value;
        }

        private _rules: TRules;

        public get rules(): TRules {
            return this._rules;
        }

        public set rules(value: TRules) {
            this._rules = value;
        }

        private _form: any;

        public get form(): any {
            return this._form;
        }

        public set form(value: any) {
            this._form = value;
        }

        public onMemberChanged(memberName: string, newValue: any, oldValue: any): void {

        }

        public onMemberChanging(memberName: string, newValue: any, oldValue: any): void {

        }

        public async onActivated(): Promise<void> {

        }
    }
}