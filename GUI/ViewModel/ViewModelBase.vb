Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Public MustInherit Class ViewModelBase
    Implements INotifyPropertyChanged, IDisposable

#Region "INotifyPropertyChanged"

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Friend Sub NotifyPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)

        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Private Sub NotifyCollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
        Select Case e.Action
            Case NotifyCollectionChangedAction.Add
            Case NotifyCollectionChangedAction.Remove
                If (e.OldItems IsNot Nothing) Then
                    For Each myitem In e.OldItems
                        If (myitem.GetType.GetInterface(NameOf(IDisposable)) IsNot Nothing) Then DirectCast(myitem, IDisposable).Dispose()
                    Next
                End If
            Case Else
                Throw New InvalidOperationException(String.Format(Globalization.CultureInfo.CurrentCulture, "Unknown Action: {0}", e.Action.ToString))
        End Select
    End Sub
#End Region

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        GC.SuppressFinalize(Me)
    End Sub
    Protected Overridable Sub Dispose(disposing As Boolean)
        If (disposedValue) Then Return

        If (disposing) Then
            ' TODO: dispose managed state (managed objects).
        End If

        ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
        ' TODO: set large fields to null.
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    Protected Overrides Sub Finalize()
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(False)
    End Sub

#End Region

End Class
Public Class Command
    Implements ICommand

    Private ReadOnly _action As Action(Of Object)
    Private ReadOnly _canExecute As Func(Of Object, Boolean)

    Sub New(action As Action(Of Object), canExecute As Func(Of Object, Boolean))
        _action = action
        _canExecute = canExecute
    End Sub

    Public Function CanExecute(parameter As Object) As Boolean Implements ICommand.CanExecute
        Return _canExecute(parameter)
    End Function

    Public Event CanExecuteChanged(sender As Object, e As EventArgs) Implements ICommand.CanExecuteChanged

    Public Sub Execute(parameter As Object) Implements ICommand.Execute
        _action(parameter)
    End Sub
End Class
