using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Themes;

namespace FlightControlPanel;

public sealed class UnitInfoViewModel : ObservableValue
{
    public UnitInfoViewModel(string name, double value, SolidColorPaint paint)
    {
        Name = name;
        Paint = paint;
        Value = value; // X, Y
    }

    public string Name { get; set; }
    public SolidColorPaint Paint { get; set; }
}

public sealed partial class MainWindowViewModel : ObservableObject
{
    public static string CoordGeneratorUri = null!;
    public static string FlightControllerRestUri = null!;
    public static string FlightControllerGrpcUri = null!;

    private bool _isReading;

    private readonly Random _random = new();

    public MainWindowViewModel()
    {
        StartCoordGeneratorCommand = new AsyncRelayCommand(StartCoordGeneratorAsync);
        StopCoordGeneratorCommand = new AsyncRelayCommand(StopCoordGeneratorAsync);
        StartFlightControllerCommand = new AsyncRelayCommand(StartFlightControllerAsync);
        StopFlightControllerCommand = new AsyncRelayCommand(StopFlightControllerAsync);
    }

    public UnitInfoViewModel[] UnitInfos
    {
        get => field;
        set
        {
            if (Equals(value, field))
                return;

            field = value;
            OnPropertyChanged();
        }
    } = [];


    public Func<ChartPoint, string> LabelsFormatter { get; set; } = point =>
    {
        var unit = (UnitInfoViewModel?) point.Context.DataSource;
        return unit is null ? string.Empty : unit.Name;
    };

    public ICommand StartCoordGeneratorCommand { get; }
    public ICommand StopCoordGeneratorCommand { get; }
    public ICommand StartFlightControllerCommand { get; }
    public ICommand StopFlightControllerCommand { get; }

    private async Task StartCoordGeneratorAsync()
    {
        try
        {
            await PostAsync(CoordGeneratorUri, "/start");
        }
        catch (Exception)
        {
            Console.WriteLine("Ошибка запуска генератора координат");
        }
    }

    private async Task StopCoordGeneratorAsync()
    {
        try
        {
            await PostAsync(CoordGeneratorUri, "/stop");
        }
        catch (Exception)
        {
            Console.WriteLine("Ошибка остановки генератора координат");
        }
    }

    private async Task StartFlightControllerAsync()
    {
        try
        {
            await PostAsync(FlightControllerRestUri, "/start");
        }
        catch (Exception)
        {
            Console.WriteLine("Ошибка запуска диспетчера полётов");
            return;
        }

        _isReading = true;

        using var grpcChannel = GrpcChannel.ForAddress(FlightControllerGrpcUri);
        var unitInfoClient = new UnitInfo.UnitInfoClient(grpcChannel);

        var paints = Enumerable.Range(0, 8)
            .Select(i => new SolidColorPaint(ColorPalletes.MaterialDesign500[i].AsSKColor()))
            .ToArray();

        while (_isReading)
        {
            var response = await unitInfoClient.ProvideAsync(new Empty());

            var uiList = UnitInfos.ToList();
            foreach (var item in response.Items)
            {
                var vm = UnitInfos.SingleOrDefault(x => x.Name == item.Name);
                if (vm is null)
                {
                    var paint = paints.FirstOrDefault(p => UnitInfos.All(i => i.Paint != p));
                    if (paint == null)
                        paint = paints[_random.Next(0, paints.Length)];

                    vm = new UnitInfoViewModel(item.Name, item.X, paint);
                    uiList.Add(vm);
                }
                vm.Value = item.X;
            }

            UnitInfos = [.. uiList.OrderBy(x => x.Value)];

            await Task.Delay(100);
        }
    }

    private async Task StopFlightControllerAsync()
    {
        _isReading = false;

        try
        {
            await PostAsync(FlightControllerRestUri, "/stop");
        }
        catch (Exception)
        {
            Console.WriteLine("Ошибка остановки диспетчера полётов");
        }
    }

    private static async Task PostAsync(string baseUri, string requestUri, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(baseUri);

        using var response = await httpClient.PostAsync(requestUri, content: null, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new Exception();
    }
}