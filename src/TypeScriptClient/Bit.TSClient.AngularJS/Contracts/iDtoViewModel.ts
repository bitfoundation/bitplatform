module Bit.Contracts {

    export interface IDtoViewModel<TDto extends Model.Contracts.IDto> {

        onMemberChanged(memberName: keyof TDto, newValue: any, oldValue: any): Promise<void>;

        onMemberChanging(memberName: keyof TDto, newValue: any, oldValue: any): Promise<void>;

    }

}