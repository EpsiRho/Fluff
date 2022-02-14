#include <iostream>

int main()
{
    // Get the input
    int input = 0;
    std::cin >> input;

    // Check if zero
    if (input == 0) {
        std::cout << "No Change";
        return 0;
    }

    // We'll go biggest to smallest, so dollar first
    int NumOfDollars = input / 100;
    input = input - (NumOfDollars * 100);

    int NumOfQuarters = input / 25;
    input = input - (NumOfQuarters * 25);

    int NumOfDimes = input / 10;
    input = input - (NumOfDimes * 10);

    int NumOfNickles = input / 5;
    input = input - (NumOfNickles * 5);

    int NumOfPennies = input / 1;
    input = input - (NumOfPennies * 1);



    // Lastly, We'll check each coin to see if it needs output

    // Dollars
    if (NumOfDollars == 1) {
        std::cout << NumOfDollars << " Dollar\n";
    }
    else if (NumOfDollars > 1) {
        std::cout << NumOfDollars << " Dollars\n";
    }

    // Quarters
    if (NumOfQuarters == 1) {
        std::cout << NumOfQuarters << " Quarter\n";
    }
    else if (NumOfQuarters > 1) {
        std::cout << NumOfQuarters << " Quarters\n";
    }

    // Dimes
    if (NumOfDimes == 1) {
        std::cout << NumOfDimes << " Dime\n";
    }
    else if (NumOfDimes > 1) {
        std::cout << NumOfDimes << " Dimes\n";
    }

    // Nickles
    if (NumOfNickles == 1) {
        std::cout << NumOfNickles << " Nickle\n";
    }
    else if (NumOfNickles > 1) {
        std::cout << NumOfNickles << " Nickles\n";
    }

    // Pennies
    if (NumOfPennies == 1) {
        std::cout << NumOfPennies << " Penny\n";
    }
    else if (NumOfPennies > 1) {
        std::cout << NumOfPennies << " Pennies\n";
    }

    return 0;
}

