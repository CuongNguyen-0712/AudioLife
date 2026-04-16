using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using VinhKhanhAudioGuide.Mobile.Services;
using ZXing.Net.Maui;
using SkiaSharp;
using ZXing.SkiaSharp;
using ZXing.Common;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class QrScannerPage : ContentPage
{
	private bool _isHandlingResult;
	private bool _isPageActive;
	private bool _scannerConfigured;
	private static readonly string[] SampleQrImageCandidates =
	{
		"qr_code.png",
		"Resources/Raw/qr_code.png",
		"Resources/Images/qr_code.png",
		"qr_code.scale-100.png"
	};
	private readonly ILocalizationService _localizationService;

	public QrScannerPage()
	{
		InitializeComponent();
		_localizationService = ResolveLocalizationService();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		_isPageActive = true;
		await StartScannerAsync();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_isPageActive = false;
		StopScanner();
	}

	private async Task StartScannerAsync()
	{
		var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
		if (status != PermissionStatus.Granted)
		{
			status = await Permissions.RequestAsync<Permissions.Camera>();
		}

		if (status != PermissionStatus.Granted)
		{
			await DisplayAlert(
				_localizationService.GetString("Common_Notice"),
				_localizationService.GetString("Qr_StatusNoCamera"),
				_localizationService.GetString("Common_Understood"));
			App.NavigateToIntro();
			return;
		}

		await ResumeScannerAsync(forceCameraRefresh: true);
	}

	private void ConfigureScannerIfNeeded()
	{
		if (_scannerConfigured)
		{
			return;
		}

		ScannerView.Options = new BarcodeReaderOptions
		{
			Formats = BarcodeFormats.TwoDimensional,
			AutoRotate = true,
			Multiple = false
		};

		ScannerView.CameraLocation = CameraLocation.Rear;
		_scannerConfigured = true;
	}

	private async Task<bool> WaitForScannerHandlerReadyAsync()
	{
		for (var i = 0; i < 20; i++)
		{
			if (ScannerView.Handler is not null)
			{
				return true;
			}

			await Task.Delay(80);
		}

		return ScannerView.Handler is not null;
	}

	private void StopScanner()
	{
		try
		{
			ScannerView.IsDetecting = false;
		}
		catch
		{
			// Ignore teardown errors when page is leaving.
		}
	}

	private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
	{
		if (_isHandlingResult || !_isPageActive)
		{
			return;
		}

		var firstResult = e.Results?.FirstOrDefault();
		var rawValue = firstResult?.Value;
		if (string.IsNullOrWhiteSpace(rawValue))
		{
			return;
		}

		_isHandlingResult = true;
		StopScanner();

		MainThread.BeginInvokeOnMainThread(async () => await HandleScannedValueAsync(rawValue));
	}

	private async Task HandleScannedValueAsync(string rawValue)
	{
		StatusLabel.Text = $"Đã quét: {rawValue}";

		if (!QrCodePayloadService.TryParseAudioPayload(rawValue, out var payload))
		{
			await DisplayAlert(
				_localizationService.GetString("Common_Notice"),
				$"QR đã đọc được nhưng không đúng định dạng deep link:\n{rawValue}",
				_localizationService.GetString("Common_Understood"));

			if (_isPageActive)
			{
				StatusLabel.Text = _localizationService.GetString("Qr_StatusInvalid");
				await ResumeScannerAsync();
			}

			return;
		}

		if (!_isPageActive)
		{
			return;
		}

		StatusLabel.Text = "Đang chuyển đến màn hình thanh toán...";
		await App.CompleteQrOnboardingAsync(payload);
	}

	private async Task ResumeScannerAsync(bool forceCameraRefresh = false)
	{
		if (!_isPageActive)
		{
			return;
		}

		ConfigureScannerIfNeeded();

		try
		{
			_isHandlingResult = false;
			StopScanner();

			if (forceCameraRefresh)
			{
				ScannerView.IsEnabled = false;
				await Task.Delay(220);
				ScannerView.IsEnabled = true;
			}

			var ready = await WaitForScannerHandlerReadyAsync();
			if (!ready || !_isPageActive)
			{
				StatusLabel.Text = _localizationService.GetString("Qr_StatusCameraFailed");
				return;
			}

			await Task.Delay(120);
			if (!_isPageActive)
			{
				return;
			}

			ScannerView.IsDetecting = true;
			StatusLabel.Text = _localizationService.GetString("Qr_StatusPrompt");
		}
		catch
		{
			StatusLabel.Text = _localizationService.GetString("Qr_StatusCameraFailed");
		}
	}

	private void OnRescanClicked(object sender, EventArgs e)
	{
		_ = ResumeScannerAsync(forceCameraRefresh: true);
		StatusLabel.Text = _localizationService.GetString("Qr_StatusPrompt");
	}

	private async void OnPickQrImageClicked(object sender, EventArgs e)
	{
		if (_isHandlingResult)
		{
			return;
		}

		try
		{
			var file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
			{
				Title = _localizationService.GetString("Qr_ImagePickerTitle")
			});

			if (file is null)
			{
				return;
			}

			await using var input = await file.OpenReadAsync();
			using var memory = new MemoryStream();
			await input.CopyToAsync(memory);
			var imageBytes = memory.ToArray();

			PickedQrImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
			PickedQrFrame.IsVisible = true;

			var qrText = TryDecodeQrTextFromImage(imageBytes);
			if (string.IsNullOrWhiteSpace(qrText))
			{
				StatusLabel.Text = _localizationService.GetString("Qr_StatusNoQrInImage");
				return;
			}

			_isHandlingResult = true;
			ScannerView.IsDetecting = false;
			await HandleScannedValueAsync(qrText);
		}
		catch
		{
			StatusLabel.Text = _localizationService.GetString("Qr_StatusImageReadFailed");
		}
	}

	private async void OnUseSampleQrCodeClicked(object sender, EventArgs e)
	{
		if (_isHandlingResult)
		{
			return;
		}

		try
		{
			await using var input = await OpenSampleQrImageStreamAsync();
			using var memory = new MemoryStream();
			await input.CopyToAsync(memory);
			var imageBytes = memory.ToArray();

			PickedQrImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
			PickedQrFrame.IsVisible = true;

			var qrText = TryDecodeQrTextFromImage(imageBytes);
			if (string.IsNullOrWhiteSpace(qrText) || !QrCodePayloadService.TryParseAudioPayload(qrText, out _))
			{
				qrText = BuildFallbackSampleDeepLink();
				StatusLabel.Text = "QR mẫu chưa đúng định dạng, đang dùng payload mẫu nội bộ...";
			}

			_isHandlingResult = true;
			StopScanner();
			await HandleScannedValueAsync(qrText);
		}
		catch
		{
			StatusLabel.Text = _localizationService.GetString("Qr_StatusImageReadFailed");
		}
	}

	private static async Task<Stream> OpenSampleQrImageStreamAsync()
	{
		foreach (var candidate in SampleQrImageCandidates)
		{
			try
			{
				return await FileSystem.OpenAppPackageFileAsync(candidate);
			}
			catch
			{
				// Try next candidate path.
			}
		}

		throw new FileNotFoundException("Sample QR image not found in app package.");
	}

	private static string BuildFallbackSampleDeepLink()
	{
		return QrCodePayloadService.BuildAudioDeepLink(
			locationId: "vk-sample-location",
			identityToken: "sample-token");
	}

	private void OnBackToIntroClicked(object sender, EventArgs e)
	{
		StopScanner();
		App.NavigateToIntro();
	}

	private static string? TryDecodeQrTextFromImage(byte[] imageBytes)
	{
		using var bitmap = SKBitmap.Decode(imageBytes);
		if (bitmap is null)
		{
			return null;
		}

		var reader = new BarcodeReader
		{
			AutoRotate = true,
			Options = new DecodingOptions
			{
				TryHarder = true,
				TryInverted = true,
				PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE }
			}
		};

		var result = reader.Decode(bitmap);
		return result?.Text;
	}

	private static ILocalizationService ResolveLocalizationService()
	{
		return Application.Current?.Handler?.MauiContext?.Services.GetService<ILocalizationService>()
			?? new LocalizationService();
	}
}
