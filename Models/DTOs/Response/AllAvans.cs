using System.ComponentModel;
using System.Runtime.CompilerServices;

public class AllAvans : INotifyPropertyChanged
{
    private int _idAvans;
    private DateOnly _date;
    private decimal _amount;
    
    private bool _hasChanges;
    private decimal _originalAmount;
    private DateOnly _originalDate;
    

    public int IdAvans
    {
        get => _idAvans;
        set { _idAvans = value; OnPropertyChanged(); }
    }

    public DateOnly Date
    {
        get => _date;
        set
        {
            if (_date != value)
            {
                _date = value;
                CheckForChanges();
                OnPropertyChanged();
            }
        }
    }

    public decimal Amount
    {
        get => _amount;
        set
        {
            if (_amount != value)
            {
                _amount = value;
                CheckForChanges();
                OnPropertyChanged();
            }
        }
    }

   

    public bool HasChanges
    {
        get => _hasChanges;
        set { _hasChanges = value; OnPropertyChanged(); }
    }

    // Метод для сохранения оригинальных значений при загрузке
    public void SetOriginalValues()
    {
        _originalDate = _date;
        _originalAmount = _amount;
        
        HasChanges = false;
    }

    // Проверка на изменения
    private void CheckForChanges()
    {
        HasChanges = (_date != _originalDate) ||
                    (_amount != _originalAmount);
                    
    }

    // Сброс изменений
    public void ResetChanges()
    {
        _date = _originalDate;
        _amount = _originalAmount;
       
        HasChanges = false;
        OnPropertyChanged(nameof(Date));
        OnPropertyChanged(nameof(Amount));
       
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}