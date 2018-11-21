function nestedUpdate(oldModelParent, oldModelKey, newModel) {
    const oldModel = oldModelParent[oldModelKey];
    if (oldModel !== newModel) {
        if (typeof newModel === "object" && !(Symbol.iterator in newModel)) {
            for (const key of Object.keys(newModel)) {
                nestedUpdate(oldModel, key, newModel[key]);
            }
        } else {
            oldModelParent[oldModelKey] = newModel;
        }
    }
}

export function mkMethod(update, mkMsg) {
    return function () {
        const newModel = update(this, this, mkMsg(arguments));
        if (this !== newModel) {
            for (const key of Object.keys(newModel)) {
                nestedUpdate(this, key, newModel[key]);
            }
        }
    }
}