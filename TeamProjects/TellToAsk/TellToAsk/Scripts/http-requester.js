﻿

window.HttpRequester = function () {
	function getJSON(serviceUrl) {
		var promise = new RSVP.Promise(function (resolve, reject) {
		    $.ajax({
				url: serviceUrl,
				type: "GET",
				dataType: "json",
				success: function (data) {
					resolve(data);
				},
				error: function (err) {
					reject(err);
				}
			});
		});
		return promise;
	}

	function postJSON(serviceUrl, data) {
		var promise = new RSVP.Promise(function (resolve, reject) {
		    $.ajax({
				url: serviceUrl,
				dataType: "json",
				type: "POST",
				contentType: "application/json",
				data: JSON.stringify(data),
				success: function (data) {
					resolve(data);
				},
				error: function (err) {
					reject(err);
				}
			});
		});
		return promise;
	}

	function putJSON(serviceUrl, data) {
	    var promise = new RSVP.Promise(function (resolve, reject) {
	        jQuery.ajax({
	            url: serviceUrl,
	            dataType: "json",
	            type: "PUT",
	            contentType: "application/json",
	            data: JSON.stringify(data),
	            success: function (data) {
	                resolve(data);
	            },
	            error: function (err) {
	                
	                reject(err);
	                console.log("ERROR", err);	
	            }
	        });
	    });
	    return promise;
	}
	return {
		getJSON: getJSON,
		postJSON: postJSON,
		putJSON: putJSON
	}
};