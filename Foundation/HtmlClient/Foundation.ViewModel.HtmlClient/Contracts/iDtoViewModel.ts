module Foundation.ViewModel.Contracts {

    export interface IDtoViewModel {

        onMemberChanged(memberName: string, newValue: any, oldValue: any): Promise<void>;

        onMemberChanging(memberName: string, newValue: any, oldValue: any): Promise<void>;

    }

}