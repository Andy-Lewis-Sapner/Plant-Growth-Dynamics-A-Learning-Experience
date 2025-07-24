package com.plantgame.server.utils;

import software.amazon.awssdk.enhanced.dynamodb.AttributeConverter;
import software.amazon.awssdk.enhanced.dynamodb.AttributeValueType;
import software.amazon.awssdk.enhanced.dynamodb.EnhancedType;
import software.amazon.awssdk.services.dynamodb.model.AttributeValue;

import java.util.HashMap;
import java.util.Map;

/**
 * A converter class responsible for transforming a Vector3 object to and from its
 * AttributeValue representation used in DynamoDB operations.
 * <p>
 * This class implements the AttributeConverter interface to provide custom
 * serialization and deserialization logic for the Vector3 class.
 * <p>
 * The Vector3 object is converted to a map-based AttributeValue with the keys "x",
 * "y", and "z" representing its components. Similarly, this map is transformed back into
 * a Vector3 instance during deserialization.
 * <p>
 * Methods:
 * - transformFrom: Converts a Vector3 instance into an AttributeValue object.
 * - transformTo: Converts an AttributeValue object back into a Vector3 instance.
 * - type: Returns the EnhancedType representing the Vector3 class.
 * - attributeValueType: Indicates the DynamoDB AttributeValue type handled by this converter.
 */
public class Vector3Converter implements AttributeConverter<Vector3> {
	@Override
	public AttributeValue transformFrom(Vector3 vector) {
		Map<String, AttributeValue> attributeMap = new HashMap<>();
		attributeMap.put("x", AttributeValue.builder().n(Float.toString(vector.getX())).build());
		attributeMap.put("y", AttributeValue.builder().n(Float.toString(vector.getY())).build());
		attributeMap.put("z", AttributeValue.builder().n(Float.toString(vector.getZ())).build());
		return AttributeValue.builder().m(attributeMap).build();
	}

	@Override
	public Vector3 transformTo(AttributeValue attributeValue) {
		Map<String, AttributeValue> attributeMap = attributeValue.m();
		float x = Float.parseFloat(attributeMap.getOrDefault("x", AttributeValue.builder().n("0").build()).n());
		float y = Float.parseFloat(attributeMap.getOrDefault("y", AttributeValue.builder().n("0").build()).n());
		float z = Float.parseFloat(attributeMap.getOrDefault("z", AttributeValue.builder().n("0").build()).n());
		return new Vector3(x, y, z);
	}

	@Override
	public EnhancedType<Vector3> type() {
		return EnhancedType.of(Vector3.class);
	}

	@Override
	public AttributeValueType attributeValueType() {
		return AttributeValueType.M;
	}
}
