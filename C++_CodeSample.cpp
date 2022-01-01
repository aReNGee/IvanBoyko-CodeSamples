//	This file contains a sample of the most intersting code I wrote for my Blue Flame Engine Capstone Project.
//  The game we built for the engine, MECC, featured four player online multiplayer.
//	To minimized the data that we needed to send back and forth, I chose to allow each client to run the game independantly.
//	The only data that we need to send between the clients to keep the game in sync is player input.
//	All of the game clients behave as though all four players/controllers are local.
//
//	I've included an overview of what indexes refer to which controller buttons on an Xbox Gamepad.
//	The code below that shows my methods for packing and unpacking input code.
//
//  This code is for illustration purposes only and cannot be executed.

///THE CONTROLLER INPUTS ARE STORED IN controllerState IN VECTOR FORM.
///
/// Index 0 holds the player index that the controller refers to, from 0 to 3.
/// Index 1/2 are the left joystick x/y and Index 3/4 are the right joystick x/y. These are stored as proper ints rather than bools.
/// Index 5 and 6 are the Left and Right Trigger respectively. 1 for pressed, 0 for not.
/// Indexes from 7 onwards are controller buttons. 1 for pressed, 0 for not. We do not store the state of the xbox key.
/// 7 is A Button
/// 8 is B Button
/// 9 is X Button
/// 10 is Y Button
/// 11 is Left Bumper
/// 12 is Right Bumper
/// 13 is Back Button
/// 14 is Start Button
/// 15 is Left Joystick Push
/// 16 is Right Joystick Push
/// Finally, we save something that isn't a button
/// 17 is Up on the DPad (1 for pressed, 0 for not)

std::string PlayerInput::UpdateJoystickState() {
	// Index 0 is player number.
	std::string networkChunk = std::to_string(playerNum) + "|"; 
	std::string temp = "";

	glm::vec2 primaryStick = LeftJoystick();
	// 1 is primary stick x axis.
	temp = std::to_string(primaryStick.x); 
	networkChunk += temp.substr(0, 5);
	temp = std::to_string(primaryStick.y);
	// 2 is primary stick y axis.
	networkChunk += temp.substr(0, 5); 

	glm::vec2 secondaryStick = RightJoystick();
	// 3 is secondary stick x.
	temp = std::to_string(secondaryStick.x); 
	networkChunk += temp.substr(0, 5);
	// 4 is secondary stick y.
	temp = std::to_string(secondaryStick.y); 
	networkChunk += temp.substr(0, 5);

	bool ltp = LeftTriggerPressed();
	// 5 is left trigger state.
	networkChunk += std::to_string(ltp); 

	bool rtp = RightTriggerPressed();
	// 6 is right trigger state.
	networkChunk += std::to_string(rtp); 

	for (int i = 0; i < 10; i++) {
		int bState = SDL_JoystickGetButton(joystick, i);
		networkChunk += std::to_string(bState);
	}

	// Send the position of the hat (DPad).
	int bState;
	if (SDL_JoystickGetHat(joystick, 0) & SDL_HAT_UP) {
		bState = 1;
	}
	else {
		bState = 0;
	}
	networkChunk += std::to_string(bState);

	return networkChunk;
}

void PlayerInput::ParseNetworkInputs(std::string inputs) {

	if (inputs.size() >= 35) {
		controllerState.clear();
		std::string outputs = inputs;
		std::string playerNumber = outputs.substr(0, 1);
		try
		{
			int playerNum = std::stoi(playerNumber);
			controllerState.push_back(playerNum);
		}
		catch (const std::exception&)
		{
			controllerState.push_back(0);
		}
		outputs = outputs.substr(2, 99);

		for (int i = 0; i < 4; i++) {
			std::string subString = outputs.substr(0, 5);
			try
			{
				int valueOfSubstring = std::stoi(subString);
				controllerState.push_back(valueOfSubstring);
			}
			catch (const std::exception&)
			{
				controllerState.push_back(0);
			}
			outputs = outputs.substr(5, 99);
		}
		for (int i = 0; i < 13; i++) {
			std::string subString = outputs.substr(0, 1);
			try
			{
				int valueOfSubstring = std::stoi(subString);
				controllerState.push_back(valueOfSubstring);
			}
			catch (const std::exception&)
			{
				controllerState.push_back(0);
			}

			outputs = outputs.substr(1, 99);
		}
	}
}