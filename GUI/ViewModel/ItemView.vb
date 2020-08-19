Imports Satisfactory.ObjectModel

Public Class ItemView
    Inherits ViewModelBase
#Region "Base Object"
    Private _baseObj As Item

    Public Property baseObj As Item
        Get
            Return _baseObj
        End Get
        Set
            _baseObj = Value
        End Set
    End Property
#End Region

#Region "Base DataStoreView"
    Private _baseStoreView As DataStoreView

    Public Property baseStoreView As DataStoreView
        Get
            Return _baseStoreView
        End Get
        Set
            _baseStoreView = Value
        End Set
    End Property
#End Region
End Class

Partial Class ItemView
#Region "Base Properties"
    Public ReadOnly Property ItemType As String
        Get
            Return baseObj.ItemType
        End Get
    End Property
#End Region
End Class

Partial Class ItemView
#Region ""

#End Region
End Class
