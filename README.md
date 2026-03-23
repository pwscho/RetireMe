1. Introduction
    RetireMe is a retirement‑planning application that helps you model your financial future year‑by‑year. It uses a transparent, deterministic simulation engine to project account balances, withdrawals, taxes, Social Security benefits, and long‑term sustainability.

    The software is designed for clarity and reproducibility. Every result is generated from explicit inputs, with no hidden assumptions.

2. Application Overview
    RetireMe consists of two major components:

    RetireMe.Core — the simulation engine

    RetireMe.UI — the Windows desktop interface (WPF)

    The UI guides you through building a scenario, entering your financial information, and running simulations. Results are displayed in a clean, tabular format suitable for reporting and charting.

3. Key Concepts
    Scenario
    A scenario is a complete set of assumptions: accounts, income, spending, Social Security, tax rules, and simulation settings. You may create multiple scenarios to compare strategies.

    ResultRow
    Each year of the simulation produces a flat row of data:

    Year

    Account balances

    Contributions

    Withdrawals

    RMDs

    Taxes

    Social Security

    Investment returns

    Ending balances

    This structure makes it easy to export, chart, or compare scenarios.

4. Getting Started
4.1 About the Application
This code is created in Visual Studio Insiders.  Copy the files to your local machine and open the solution in Visual Studio.  
Build the solution to restore NuGet packages and compile the code.  Then set RetireMe.UI as the startup project and run the application.


4.2 Creating Your First Scenario
    When the application opens:

    Select New Scenario

    Give your scenario a name

    Proceed through each tab to enter your information

    You can duplicate scenarios to test variations.

5. User Interface Guide
    The UI is organized into tabs. Work through them from left to right.

5.1 Scenario Tab
    Here you define the high‑level parameters:

    Previous years tax values (assumes that you are starting the simulation in a future year and want to carry forward tax history)
    
    Simulation start year

    Inflation assumptions

    Spending needs 
        
    Spending growth plan (Fixed = spending * inflation), Historical = uses historical inflation rates, Guardrail = adjust inflation based on portfolio performance)

    This tab establishes the “rules of the world” for your scenario.

5.2 Accounts Tab
    Account types are created automatically based on Social Security inputs.  Accounts are categorized as Tax Deferred (pre‑tax), Tax Free (post‑tax), and Taxable.

    Enter all your financial accounts:

    Tax Deferred/ 401(k)

    Tax Free / Roth 401(k), Cash

    Taxable / brokerage

    Cash reserves

    For each account type, specify:

    Starting balance

    Expected return

    Priority for withdrawals (system will withdraw from accounts in priority order until exausted, then move to the next account)



5.3 Social Security Tab
    Enter Social Security details for each household member (Primary and Spouse):

    Current Age

    Benefit start age

    Primary Insurance Amount (PIA)

    COLA assumptions

    RetireMe calculates annual benefits and integrates them into taxable income.

5.4 Income Tab
    Add any non‑Social‑Security income, specify if income is taxable as ordinary income:

    Employment income

    Rental income

    Pension income

    Other recurring income streams

    Specify start and end years for each income source.

5.5 Withdrawals Tab

Special withdrawals (e.g., one‑time expenses) (inflation does not apply to these)



5.6 Roth Conversions Tab
    If you plan to convert funds from Traditional to Roth accounts:

    Enter conversion amounts

    Specify years or ranges

    The tax engine automatically incorporates conversion taxes.

5.7 Results Tab
    After running the simulation, the Results tab displays:

    Year‑by‑year account balances

    Withdrawals and contributions

    RMDs

    Taxes

    Social Security

    Investment returns

    Ending balances

    Any guardrail adjustments (will show up in base spending)

    You can scroll, filter, or export results for reporting.

6. Running a Simulation
    Once all inputs are complete:

    Click Run Simulation (runs a fixed simulation only, one-pass using parameters defined in the Scenario tab)

    The engine processes each year sequentially

    Results appear instantly in the Results tab

    Account balances (based on owner and tax brackets) are shown in the Owner/Tax Bracket Summary Tab.

    Because the engine uses a flat ResultRow structure, simulations are fast and memory‑efficient.

7. Understanding the Output
    Each row represents one year of your financial life. Key fields include:

    StartingBalance — total portfolio at the beginning of the year

    InvestmentReturn — growth based on your return assumptions

    Withdrawal — spending taken from accounts

    TaxesDue — federal taxes calculated using year‑specific rules

    RMD — Required Minimum Distributions

    MAGI — Modified Adjusted Gross Income

    IrmaaPaid — Medicare IRMAA surcharges (if modeled)

    EndingBalance — total portfolio at year end

    These values allow you to evaluate sustainability, tax efficiency, and risk.

8. Generating a Report

    After running a scenario, RetireMe automatically compiles all simulation results into a single Results Summary window. 
    This summary includes the fixed, historical, and Monte Carlo simulations, along with key statistics such as success rates, average and median ending balances, 
    tax totals, and total spending. The summary also provides detailed year‑by‑year results in sortable tables and generates visual charts, including income and spending breakdowns, 
    Monte Carlo histograms, and percentile trajectories. If you want to save or analyze your results outside the application, you can select Export to Excel, 
    which creates a multi‑sheet workbook containing your scenario settings and the full set of simulation rows for each method. 
    This makes it easy to archive your work, compare scenarios, or perform additional analysis in spreadsheet software.

9.  About the Software
    RetireMe is a lightweight, transparent, and reproducible retirement simulation engine designed for long‑term maintainability and clarity.
