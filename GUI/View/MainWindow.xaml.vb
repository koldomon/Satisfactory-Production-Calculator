Imports System.ComponentModel
Imports System.Windows.Threading

Class MainWindow
    Private Sub AddProduction_Click(sender As Object, e As RoutedEventArgs)
        Dim myDataStore = DirectCast(Me.DataContext, DataStoreView)

        Dim myItemsPerMinuteString = txtAddItemsPerMinute.Text.Trim
        Dim myItemsPerMinuteDouble As Double
        If Double.TryParse(myItemsPerMinuteString, myItemsPerMinuteDouble) Then
            Dim myProduction = myDataStore.AddProduction(DirectCast(cbAddRecipe.SelectedItem, RecipeView), myItemsPerMinuteDouble)
            myProduction.IsSelected = True
        End If
    End Sub
    Private Sub SelectedProduction_Changed(sender As Object, e As RoutedPropertyChangedEventArgs(Of Object))
        DirectCast(Me.DataContext, DataStoreView).SelectedProduction = DirectCast(e.NewValue, ProductionView)
    End Sub

    Friend Sub SetWaitCursor()
        Dispatcher.Invoke(Sub() Me.Cursor = Cursors.Wait, DispatcherPriority.Input, Nothing)
    End Sub
    Friend Sub InitReloadUi()
        Dispatcher.BeginInvoke(Sub() My.Application.ReloadUI(), DispatcherPriority.ApplicationIdle, Nothing)
        Dispatcher.BeginInvoke(Sub() Me.Cursor = Cursors.Arrow, DispatcherPriority.Input, Nothing)
    End Sub

    Private Sub txtAddItemsPerMinute_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles txtAddItemsPerMinute.PreviewKeyDown
        If e.Key.Equals(Key.Enter) Then
            AddProduction_Click(sender, New RoutedEventArgs())
        End If
    End Sub

    Private Sub AppCmd_Open_CanExecute(sender As Object, e As CanExecuteRoutedEventArgs)
        e.CanExecute = True
        e.Handled = True
    End Sub

    Private Sub AppCmd_Open_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        If (Me.DataContext IsNot Nothing) AndAlso (Me.DataContext.GetType.Equals(GetType(DataStoreView))) Then
            Dim myDataStore = DirectCast(Me.DataContext, DataStoreView)
            If myDataStore.HasChanged Then
                Dim myResult = MsgBox("You didn't saved your changes. Are you sure to continue?", MsgBoxStyle.Question Or MsgBoxStyle.YesNo, "Unsaved changes")
                If (myResult = MsgBoxResult.No) Then Exit Sub
            End If
            My.Application.LoadProductions(myDataStore.CurrentFile)
        End If
    End Sub

    Private Sub AppCmd_Save_CanExecute(sender As Object, e As CanExecuteRoutedEventArgs)
        If (Me.DataContext IsNot Nothing) AndAlso (Me.DataContext.GetType.Equals(GetType(DataStoreView))) Then
            Dim myDataStore = DirectCast(Me.DataContext, DataStoreView)
            If (myDataStore.HasChanged) Then e.CanExecute = True
        End If
        e.Handled = True
    End Sub

    Private Sub AppCmd_Save_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        If (Me.DataContext IsNot Nothing) AndAlso (Me.DataContext.GetType.Equals(GetType(DataStoreView))) Then
            Dim myDataStore = DirectCast(Me.DataContext, DataStoreView)
            If myDataStore.HasChanged Then
                My.Application.SaveProduction(myDataStore.CurrentFile)
            End If
        End If
    End Sub

    Private Sub AppCmd_SaveAs_CanExecute(sender As Object, e As CanExecuteRoutedEventArgs)
        If (Me.DataContext IsNot Nothing) AndAlso (Me.DataContext.GetType.Equals(GetType(DataStoreView))) Then
            Dim myDataStore = DirectCast(Me.DataContext, DataStoreView)
            If (myDataStore.HasChanged) Then e.CanExecute = True
        End If
        e.Handled = True
    End Sub

    Private Sub AppCmd_SaveAs_Executed(sender As Object, e As ExecutedRoutedEventArgs)
        If (Me.DataContext IsNot Nothing) AndAlso (Me.DataContext.GetType.Equals(GetType(DataStoreView))) Then
            Dim myDataStore = DirectCast(Me.DataContext, DataStoreView)
            If myDataStore.HasChanged Then
                myDataStore.GetNewFile()
                My.Application.SaveProduction(myDataStore.CurrentFile)
            End If
        End If
    End Sub
End Class
