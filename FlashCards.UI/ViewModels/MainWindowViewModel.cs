using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using FlashCards.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FlashCards.UI.Views;
using System.Collections.Generic;

namespace FlashCards.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ApplicationDataContext? dbContext;

    #region Properties
    [ObservableProperty]
    private ObservableCollection<CardViewModel> cards = new();

    [ObservableProperty]
    private string questionInput = string.Empty;

    [ObservableProperty]
    private string answerInput = string.Empty;

    [ObservableProperty]
    private string tagsInput = string.Empty;

    [ObservableProperty]
    private CardViewModel? selectedCard;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int totalTestCards;

    [ObservableProperty]
    private bool isAnswerVisible;

    [ObservableProperty]
    private bool isInTestMode;

    [ObservableProperty]
    private bool isTestFinished;

    [ObservableProperty]
    private CardViewModel? currentTestCard;

    [ObservableProperty]
    private bool isAnswerVisibleInTest;

    [ObservableProperty]
    private int currentTestIndex;

    [ObservableProperty]
    private int correctAnswers;

    #endregion

    #region Commands
    public IRelayCommand StartTestCommand { get; }
    public IRelayCommand ShowTestAnswerCommand { get; }
    public IRelayCommand CorrectTestAnswerCommand { get; }
    public IRelayCommand WrongTestAnswerCommand { get; }
    public IRelayCommand ExitTestCommand { get; }
    public IRelayCommand AddCardAsyncCommand { get; }
    public IRelayCommand UpdateCardAsyncCommand { get; }
    public IRelayCommand<CardViewModel> DeleteCardAsyncCommand { get; }
    public IRelayCommand<CardViewModel> EditSelectedCommand { get; }
    public IRelayCommand ClearInputsCommand { get; }
    public IRelayCommand FilterAsyncCommand { get; }
    public IRelayCommand ClearFilterAsyncCommand { get; }
    public IRelayCommand OpenTestWindowCommand { get; }
    #endregion

    private List<CardViewModel> testCards = new();

    public MainWindowViewModel(IDbContextFactory<ApplicationDataContext> contextFactory, IConfiguration configuration)
    {
        dbContext = contextFactory.CreateDbContext();

        AddCardAsyncCommand = new RelayCommand(async () => await AddCardAsync());
        UpdateCardAsyncCommand = new RelayCommand(async () => await UpdateCardAsync());
        DeleteCardAsyncCommand = new RelayCommand<CardViewModel>(async (card) => await DeleteCardAsync(card));
        EditSelectedCommand = new RelayCommand<CardViewModel>(EditSelected);
        ClearInputsCommand = new RelayCommand(ClearInputs);
        FilterAsyncCommand = new RelayCommand(async () => await FilterAsync());
        ClearFilterAsyncCommand = new RelayCommand(async () => await ClearFilterAsync());
        StartTestCommand = new RelayCommand(StartTest);
        ShowTestAnswerCommand = new RelayCommand(() => IsAnswerVisibleInTest = true);
        CorrectTestAnswerCommand = new RelayCommand(CorrectAnswer);
        WrongTestAnswerCommand = new RelayCommand(WrongAnswer);
        ExitTestCommand = new RelayCommand(ExitTest);


        _ = LoadDataAsync();
    }

    private async Task AddCardAsync()
    {
        if (dbContext == null || string.IsNullOrWhiteSpace(QuestionInput) || string.IsNullOrWhiteSpace(AnswerInput))
            return;

        var newCard = new Card
        {
            Question = QuestionInput,
            Answer = AnswerInput,
            Tags = TagsInput,
        };

        dbContext.Cards.Add(newCard);
        await dbContext.SaveChangesAsync();
        await LoadDataAsync();
        ClearInputs();
    }

    private async Task UpdateCardAsync()
    {
        if (dbContext == null || SelectedCard == null) return;

        var card = await dbContext.Cards.FindAsync(SelectedCard.Id);
        if (card != null)
        {
            card.Question = QuestionInput;
            card.Answer = AnswerInput;
            card.Tags = TagsInput;
            await dbContext.SaveChangesAsync();
        }

        await LoadDataAsync();
        ClearInputs();
    }

    private async Task DeleteCardAsync(CardViewModel? card)
    {
        if (dbContext == null || card == null) return;

        var dbCard = await dbContext.Cards.FindAsync(card.Id);
        if (dbCard != null)
        {
            dbContext.Cards.Remove(dbCard);
            await dbContext.SaveChangesAsync();
        }

        await LoadDataAsync();
    }

    private void EditSelected(CardViewModel? card)
    {
        if (card == null) return;
        SelectedCard = card;
        QuestionInput = card.Question;
        AnswerInput = card.Answer;
        TagsInput = card.Tags;
    }

    private void ClearInputs()
    {
        QuestionInput = string.Empty;
        AnswerInput = string.Empty;
        TagsInput = string.Empty;
    }

    private async Task FilterAsync()
    {
        if (dbContext == null) return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadDataAsync();
            return;
        }

        var lower = SearchText.ToLower();
        var filtered = await dbContext.Cards
            .Where(c => c.Question.ToLower().Contains(lower)
                     || c.Answer.ToLower().Contains(lower)
                     || c.Tags.ToLower().Contains(lower))
            .AsNoTracking()
            .ToListAsync();

        Cards.Clear();
        foreach (var c in filtered)
        {
            Cards.Add(new CardViewModel
            {
                Id = c.Id,
                Question = c.Question,
                Answer = c.Answer,
                Tags = c.Tags,
            });
        }
    }

    private async Task ClearFilterAsync()
    {
        SearchText = string.Empty;
        await LoadDataAsync();
    }

    private void StartTest()
    {
        if (Cards.Count == 0) return;

        IsInTestMode = true;
        IsTestFinished = false;
        CorrectAnswers = 0;
        CurrentTestIndex = 0;
        CurrentTestCard = Cards[CurrentTestIndex];
        IsAnswerVisibleInTest = false;
    }

    private void CorrectAnswer()
    {
        CorrectAnswers++;
        NextTestCard();
    }

    private void WrongAnswer()
    {
        NextTestCard();
    }

    private void NextTestCard()
    {
        CurrentTestIndex++;
        if (CurrentTestIndex < Cards.Count)
        {
            CurrentTestCard = Cards[CurrentTestIndex];
            IsAnswerVisibleInTest = false;
        }
        else
        {
            IsTestFinished = true;
        }
    }

    private void ExitTest()
    {
        IsInTestMode = false;
        IsTestFinished = false;
    }


    private async Task LoadDataAsync()
    {
        if (dbContext == null) return;

        var allCards = await dbContext.Cards.AsNoTracking().ToListAsync();
        Cards.Clear();
        foreach (var c in allCards)
        {
            Cards.Add(new CardViewModel
            {
                Id = c.Id,
                Question = c.Question,
                Answer = c.Answer,
                Tags = c.Tags,
                IsAnswerVisible = false
            });
        }
    }
}

public class CardViewModel
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public int ReviewCount { get; set; }
    public double Score { get; set; }
    public bool IsAnswerVisible { get; set; }
}
