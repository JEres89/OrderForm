//import 'jquery'; // comment when running or scripts will break(?!?), uncomment to get jquery intellisense

var toggleSpeed = "fast";
function ToggleDisplay(targetId,speed) {
	$(`#${targetId}`).toggle(speed??toggleSpeed);
}
function ToggleChildren(sourceElement/*: Element*/, speed) {
	$(sourceElement).children().toggle(speed ?? toggleSpeed);
}
function ToggleChildrenOfId(targetId, speed) {
	$(`#${targetId}`).children().toggle(speed ?? toggleSpeed);
}

function ToggleChildrenAndTarget(sourceElement/*: Element*/, targetId) {
	$(sourceElement.children).toggle();
	if (targetId) {
		ToggleDisplay(targetId);
	}
}
function ToggleThisAndTarget(sourceElement/*: Element*/, targetId, focusTarget = true) {
	$(sourceElement).toggle(0);
	if (targetId) {
		ToggleDisplay(targetId,0);
		if (focusTarget) {
			document.getElementById(targetId).focus();
		}
	}
}
function ShowOnlySelectedSection(select) {
	let id = select[select.selectedIndex].value;
	if (id === "all") {
		$(".formSection").each(function () {
			$(this).show(toggleSpeed);
		});
	}
	else {
		$(".formSection").each(function () {
			if ($(this).attr("id") === id) {
				$(this).show(toggleSpeed);
			}
			else { $(this).hide(toggleSpeed); }
		});
		$("#formContainer").trigger("sectionsToggled");
		toggleSpeed = "fast";
	}
}
function ShowDialog(id) {
	document.getElementById(id).showModal();
}
function CloseDialog(id) {
	document.getElementById(id).close();
}
function SetElementValue(targetId, value) {
	$(`#${targetId}`).val(value);
}
function SetSelections(targetLabelId, value) {
	let element = $(`#${targetLabelId} > select:first`);
	element.val(value);
}
function FocusOnElementId(sectionId, elementId) {
	toggleSpeed = 0;
	$("#showSection").val("S_" + sectionId).change();
	$("#formContainer").one("sectionsToggled", () => $("#formContainer").scrollTop($("#" + elementId).position().top));
}
function ValidateTextInput(target) {
	if (target.type == "text") {
		return;
	}
	let filterPattern = new RegExp(target.attributes.getNamedItem("filterPattern").value, 'g');
	let filteredValue = target.value.replaceAll(filterPattern, '');
	if (filteredValue === "") {
		target.value = filteredValue;
		return;
	}
	if (filteredValue !== target.value) {
		target.value = filteredValue;
	}
	return;
	//let matchPattern = new RegExp(target.attributes.getNamedItem("formatPattern").value);
	//if (matchPattern.test(filteredValue)) {
	//	target.value = filteredValue;

	//}
	//else {
	//	let arr = filteredValue.match(matchPattern);
	//	console.log(arr);
	//}
}
