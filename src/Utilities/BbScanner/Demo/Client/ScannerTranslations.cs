using System.Globalization;

namespace Bit.BbScanner.Demo.Client;

/// <summary>
/// The scanner's texts in the languages the customisation page offers. An application would
/// usually build these from its own resources - <c>IStringLocalizer</c>, a resx file - once per
/// language at startup.
/// </summary>
public static class ScannerTranslations
{
    /// <summary>Spanish.</summary>
    public static BbScannerTexts Spanish { get; } = new()
    {
        StartButton = "Iniciar",
        StopButton = "Detener",
        PauseButton = "Pausar",
        ResumeButton = "Reanudar",
        TorchButton = "Linterna",
        ScanImageButton = "Escanear imagen",
        CameraLabel = "Cámara",
        ZoomLabel = "Zoom",
        UnnamedCamera = number => string.Create(CultureInfo.InvariantCulture, $"Cámara {number}"),
        StatusIdle = "Listo",
        StatusStarting = "Iniciando la cámara",
        StatusScanning = "Escaneando",
        StatusPaused = "En pausa",
        StatusStopped = "Detenido",
        StatusFaulted = "Error",
        RequestingCamera = "Solicitando acceso a la cámara...",
        ReadingImage = "Leyendo la imagen...",
        NoBarcodeFound = "No se encontró ningún código en la imagen.",
        NotAnImage = "El archivo seleccionado no es una imagen.",
        ImageUnreadable = "El archivo no se pudo leer como imagen.",
        ImageNotScanned = "No se pudo procesar la imagen.",
        ImageTooLarge = megabytes => string.Create(CultureInfo.GetCultureInfo("es"), $"La imagen supera el máximo de {megabytes:0.#} MB."),
        ImageTooManyPixels = "La imagen es demasiado grande para escanearla.",
        ErrorMessage = exception => exception.Kind switch
        {
            BbScannerErrorKind.PermissionDenied => "Se denegó el permiso para usar la cámara.",
            BbScannerErrorKind.NoCamera => "No se encontró ninguna cámara.",
            BbScannerErrorKind.CameraUnavailable => "No se pudo usar la cámara; puede que otra aplicación la esté usando.",
            BbScannerErrorKind.ConstraintsUnsatisfied => "Ninguna cámara cumple lo solicitado.",
            BbScannerErrorKind.NotSupported => "Este navegador no permite usar la cámara en esta página.",
            BbScannerErrorKind.InvalidImage => "No se pudo escanear la imagen.",
            _ => "No se pudo iniciar la cámara.",
        },
        ScanAnnouncement = result => $"Leído {result.Format}: {result.Text}",
    };

    /// <summary>Persian, which is written right to left.</summary>
    public static BbScannerTexts Persian { get; } = new()
    {
        StartButton = "شروع",
        StopButton = "توقف",
        PauseButton = "مکث",
        ResumeButton = "ادامه",
        TorchButton = "چراغ‌قوه",
        ScanImageButton = "اسکن تصویر",
        CameraLabel = "دوربین",
        ZoomLabel = "بزرگ‌نمایی",
        UnnamedCamera = number => string.Create(CultureInfo.GetCultureInfo("fa"), $"دوربین {number}"),
        StatusIdle = "آماده",
        StatusStarting = "در حال روشن کردن دوربین",
        StatusScanning = "در حال اسکن",
        StatusPaused = "مکث شده",
        StatusStopped = "متوقف شد",
        StatusFaulted = "خطا",
        RequestingCamera = "در حال درخواست دسترسی به دوربین...",
        ReadingImage = "در حال خواندن تصویر...",
        NoBarcodeFound = "بارکدی در این تصویر پیدا نشد.",
        NotAnImage = "فایل انتخاب‌شده تصویر نیست.",
        ImageUnreadable = "فایل به‌عنوان تصویر خوانده نشد.",
        ImageNotScanned = "پردازش تصویر ممکن نشد.",
        ImageTooLarge = megabytes => string.Create(CultureInfo.GetCultureInfo("fa"), $"حجم تصویر از {megabytes:0.#} مگابایت مجاز بیشتر است."),
        ImageTooManyPixels = "تصویر برای اسکن بیش از حد بزرگ است.",
        ErrorMessage = exception => exception.Kind switch
        {
            BbScannerErrorKind.PermissionDenied => "اجازهٔ دسترسی به دوربین داده نشد.",
            BbScannerErrorKind.NoCamera => "دوربینی پیدا نشد.",
            BbScannerErrorKind.CameraUnavailable => "دوربین در دسترس نیست؛ شاید برنامهٔ دیگری از آن استفاده می‌کند.",
            BbScannerErrorKind.ConstraintsUnsatisfied => "هیچ دوربینی با تنظیمات درخواست‌شده سازگار نیست.",
            BbScannerErrorKind.NotSupported => "این مرورگر اجازهٔ استفاده از دوربین را در این صفحه نمی‌دهد.",
            BbScannerErrorKind.InvalidImage => "اسکن تصویر ممکن نشد.",
            _ => "دوربین روشن نشد.",
        },
        ScanAnnouncement = result => $"خوانده شد، {result.Format}: {result.Text}",
    };
}
