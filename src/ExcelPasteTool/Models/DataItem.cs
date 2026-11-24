using System.ComponentModel;

namespace ExcelPasteTool;

public class DataItem : INotifyPropertyChanged
{
    private int _rowNumber;
    private string _value = string.Empty;

    public int RowNumber
    {
        get => _rowNumber;
        set
        {
            if (_rowNumber != value)
            {
                _rowNumber = value;
                OnPropertyChanged(nameof(RowNumber));
                OnPropertyChanged(nameof(IsEven));
            }
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value ?? string.Empty;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public bool IsEven => RowNumber % 2 == 0;

    private bool _isSelected;

    /// <summary>資料表中目前選取的一列（按 Insert 會在它下面插入一筆）。</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    /// <summary>剛新增、還沒確認內容的一筆；確認時空白或重複就直接移除。</summary>
    public bool IsNew { get; set; }

    private bool _isEditing;

    /// <summary>是否正在資料表裡直接編輯這一筆（雙擊進入，Enter / 離開確認，Esc 取消）。</summary>
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (_isEditing != value)
            {
                _isEditing = value;
                OnPropertyChanged(nameof(IsEditing));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public DataItem(int rowNumber, string value)
    {
        RowNumber = rowNumber;
        Value = value;
    }

    // 讓外部可在主題或語言切換後，強制刷新與樣式相關的屬性（例如行底色）
    public void NotifyStyleChanged()
    {
        OnPropertyChanged(nameof(IsEven));
    }
}