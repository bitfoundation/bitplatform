﻿namespace Bit.BlazorUI;

public class BitModalReference
{
    public string Id { get; init; }

    public object? Content { get; private set; }

    public RenderFragment? Modal { get; set; }



    private readonly BitModalService _modalService;


    public BitModalReference(BitModalService modalService)
    {
        Id = BitShortId.NewId();
        _modalService = modalService;
    }

    public void SetContent(object content)
    {
        Content = content;
    }

    public void SetModal(RenderFragment modal)
    {
        Modal = modal;
    }

    public void Close()
    {
        _modalService.Close(this);
    }
}
