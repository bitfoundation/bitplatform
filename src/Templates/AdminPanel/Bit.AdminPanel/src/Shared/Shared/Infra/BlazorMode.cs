namespace AdminPanel.Shared.Infra;

public enum BlazorMode
{
    BlazorServer = 0,
    BlazorWebAssembly = 1,
    BlazorHybrid = 2,
#if BlazorElectron
    BlazorElectron = 0,
#else
    BlazorElectron = 3
#endif
}
