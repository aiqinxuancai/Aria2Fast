using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;

public static class ObservableExtensions
{
    public static IDisposable SubscribeOnMainThread<T>(
        this IObservable<T> source,
        Action<T> onNext)
    {
        return source.Subscribe(item =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                onNext(item);
            });
        });
    }
}