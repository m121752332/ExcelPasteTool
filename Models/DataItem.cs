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
}