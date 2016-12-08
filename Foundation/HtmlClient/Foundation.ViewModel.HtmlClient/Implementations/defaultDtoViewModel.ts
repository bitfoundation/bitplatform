module Foundation.ViewModel.Implementations {

    export type DtoFormController<TDto extends Model.Contracts.IDto> = {
        readonly[Prop in keyof TDto]: ng.INgModelController;
    } & IDtoFormController<TDto>;

    export interface IDtoFormController<TDto extends Model.Contracts.IDto> extends ng.IFormController {
        isValid(): boolean;
        editable(propName: keyof TDto, isEditable?: boolean): boolean;
        visible(propName: keyof TDto, isVisible?: boolean): boolean;
    }

    export class DefaultDtoViewModel<TDto extends Foundation.Model.Contracts.IDto, TRules extends DtoRules<TDto>> implements Contracts.IDtoViewModel {

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

        private _form: DtoFormController<TDto>;

        public get form(): DtoFormController<TDto> {
            return this._form;
        }

        public set form(value: DtoFormController<TDto>) {
            this._form = value;
        }

        public async onMemberChanged(memberName: string, newValue: any, oldValue: any): Promise<void> {

        }

        public async onMemberChanging(memberName: string, newValue: any, oldValue: any): Promise<void> {

        }

        public async onActivated(): Promise<void> {

        }
    }
}