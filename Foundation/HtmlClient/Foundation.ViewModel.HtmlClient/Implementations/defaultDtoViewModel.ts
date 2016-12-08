module Foundation.ViewModel.Implementations {

    export type DtoFormController<TDto extends Model.Contracts.IDto> = {
        readonly[Prop in keyof TDto]: IDtoModelController;
    } & IDtoFormController;

    export interface IDtoModelController extends ng.INgModelController {
        editable: boolean;
        visible: boolean;
    }

    export interface IDtoFormController extends ng.IFormController {
        isValid(): boolean;
    }

    export class DefaultDtoViewModel<TDto extends Foundation.Model.Contracts.IDto, TRules extends DtoRules<TDto>> implements Contracts.IDtoViewModel<TDto> {

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

        public async onMemberChanged(memberName: keyof TDto, newValue: any, oldValue: any): Promise<void> {

        }

        public async onMemberChanging(memberName: keyof TDto, newValue: any, oldValue: any): Promise<void> {

        }

        public async onActivated(): Promise<void> {

        }
    }
}