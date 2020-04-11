Imports System.ComponentModel
Imports System.Windows.Threading

Class MainWindow
    Private Sub AddProduction_Click(sender As Object, e As RoutedEventArgs)
        Dim myViewModel = DirectCast(Me.DataContext, MainViewModel)
        Dim myProduction = myViewModel.AddProduction(cbAddRecipe.SelectedItem, txtAddItemsPerMinute.Text)
        myProduction.IsSelected = True
    End Sub
    Private Sub SelectedProduction_Changed(sender As Object, e As RoutedPropertyChangedEventArgs(Of Object))
        DirectCast(Me.DataContext, MainViewModel).SelectedProduction = e.NewValue
    End Sub

    Friend Sub SetWaitCursor()
        Dispatcher.Invoke(Sub() Me.Cursor = Cursors.Wait, DispatcherPriority.Input, Nothing)
    End Sub
    Friend Sub InitReloadUi()
        Dispatcher.BeginInvoke(Sub() My.Application.ReloadUI(), DispatcherPriority.ApplicationIdle, Nothing)
        Dispatcher.BeginInvoke(Sub() Me.Cursor = Cursors.Arrow, DispatcherPriority.Input, Nothing)
    End Sub
End Class
