Imports Satisfactory.ObjectModel
Public Class SourceItemView
    Inherits ViewModelBase
#Region "Base Object"
    Private _baseObj As SourceItem

    Public Property baseObj As SourceItem
        Get
            Return _baseObj
        End Get
        Set
            _baseObj = Value
        End Set
    End Property
#End Region

End Class
Partial Class SourceItemView
#Region "Base Properties"
    Public ReadOnly Property ItemType As String
        Get
            Return baseObj.ItemType
        End Get
    End Property
    Public ReadOnly Property ItemsPerMinute As Double
        Get
            Return baseObj.ItemsPerMinute
        End Get
    End Property
#End Region
End Class

Partial Class SourceItemView
#Region ""

#End Region
End Class
