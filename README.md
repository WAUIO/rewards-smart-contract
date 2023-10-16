# Voxiberate Rewards Contract

This is the Voxiberate Rewards Contract, a smart contract written in C# for the Neo blockchain platform. It allows for the management and distribution of rewards to citizens. With thanks to GrantShares for providing us with a grant to develop web3 features for Voxiberate.

## Table of Contents

- [Introduction](#introduction)
- [Prerequisites](#prerequisites)
- [Features](#features)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Events](#events)
- [Contributing](#contributing)
- [License](#license)

## Introduction

The Voxiberate Rewards Contract is designed to facilitate the distribution of rewards to citizens. It provides functions for storing rewards, refunding rewards, locking rewards, submitting citizens, and distributing rewards.

## Prerequisites

[.NET Core 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

Please refer here for [Neo Blockchain Toolkit for .NET Quickstart](https://github.com/neo-project/neo-blockchain-toolkit/blob/master/quickstart.md)

You will need to copy the smart contract under the neo-express toolkit src.

Useful link [tutorial](https://developers.neo.org/tutorials/2021/05/27/getting-started-with-the-neo-blockchain-toolkit)

## Features

- Store rewards with facilitator, token, amount, and state information
- Refund rewards to facilitators
- Lock rewards to prevent further modifications
- Submit citizens for rewards
- Distribute rewards to top 5 citizens and full participation citizens

## Getting Started

To use the Voxiberate Rewards Contract, you will need the following:

- Neo blockchain platform
- Neo Smart Contract development environment
- Voxiberate Rewards Contract source code

## Usage

1. Build the smart contract 
```
$ dotnet build

MSBuild version 17.3.2+561848881 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  RewardsContract -> /home/hasinapic/Workspaces/WAUIO/VOXIBERATE/rewards-smart-contract/src/bin/sc/RewardsContract.nef
  RewardsContract -> /home/hasinapic/Workspaces/WAUIO/VOXIBERATE/rewards-smart-contract/src/bin/Debug/net6.0/RewardsContract.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:03.17
```
2. Deploy the Voxiberate Rewards Contract on the Neo blockchain platform.

[Testnet deployed smart contract](https://testnet.neotube.io/contract/0xc6e193931705ab8524da4280c9000bc57641be8c)

3. Call the various functions provided by the contract to manage and distribute rewards.
4. Listen to the events emitted by the contract to track reward-related activities.

## Events

The Voxiberate Rewards Contract emits the following events:

- `RewardStored`: Triggered when a reward is stored with facilitator, token, and amount information.
- `RewardRefunded`: Triggered when a reward is refunded to a facilitator.
- `CitizenSubmitted`: Triggered when citizens are submitted for a reward.
- `RewardDistributed`: Triggered when rewards are distributed to citizens.
- `AdminAdded`: Triggered when a new admin is added to the contract.
- `AdminRemoved`: Triggered when an admin is removed from the contract.
- `RewardLocked`: Triggered when a reward is locked.

## Contributing

Contributions to the Voxiberate Rewards Contract are welcome! If you find any issues or have suggestions for improvements, please open an issue or submit a pull request.

## License

The Voxiberate Rewards Contract is released under the [MIT License](LICENSE).
